import uuid
from datetime import datetime, timezone

from sqlalchemy.orm import Session

from app.core.models import Farm, Field, Settings, User, Zone
from app.core.schemas import FieldCreate, FieldUpdate, SettingsUpdate, ZoneCreate, ZoneUpdate

# Grace window past a zone's irrigation_interval_days before "due" becomes
# "overdue" — a fixed constant by design (not farm-configurable via Settings).
STATUS_DUE_GRACE_DAYS = 3


def get_health_status() -> str:
    return "ok"


def update_user_language(db: Session, user: User, language: str) -> User:
    user.preferred_language = language
    db.commit()
    db.refresh(user)
    return user


def get_the_settings(db: Session) -> Settings:
    settings = db.query(Settings).first()
    if settings is None:
        raise ValueError(
            "No settings row exists — check that the seed migration ran (core_add_settings_table)."
        )
    return settings


def update_settings(db: Session, settings: Settings, payload: SettingsUpdate) -> Settings:
    for key, value in payload.model_dump(exclude_unset=True).items():
        setattr(settings, key, value)
    db.commit()
    db.refresh(settings)
    return settings


def get_the_farm(db: Session) -> Farm:
    farm = db.query(Farm).first()
    if farm is None:
        raise ValueError(
            "No farm row exists — check that the seed migration ran (core_add_farm_field_zone_tables)."
        )
    return farm


def list_fields(db: Session, include_inactive: bool = False) -> list[Field]:
    query = db.query(Field)
    if not include_inactive:
        query = query.filter(Field.is_active.is_(True))
    return query.order_by(Field.name).all()


def get_field(db: Session, field_id: uuid.UUID) -> Field | None:
    return db.query(Field).filter(Field.id == field_id).first()


def create_field(db: Session, payload: FieldCreate) -> Field:
    farm = get_the_farm(db)
    field = Field(farm_id=farm.id, name=payload.name, area_ha=payload.area_ha, notes=payload.notes)
    db.add(field)
    db.commit()
    db.refresh(field)
    return field


def update_field(db: Session, field: Field, payload: FieldUpdate) -> Field:
    for key, value in payload.model_dump(exclude_unset=True).items():
        setattr(field, key, value)
    db.commit()
    db.refresh(field)
    return field


def archive_field(db: Session, field: Field) -> Field:
    field.is_active = False
    db.commit()
    db.refresh(field)
    return field


def _compute_zone_status(interval_days: int | None, last_activity_at: datetime) -> str:
    if interval_days is None:
        return "on-schedule"

    days_since = (datetime.now(timezone.utc) - last_activity_at).days
    if days_since < interval_days:
        return "on-schedule"
    if days_since < interval_days + STATUS_DUE_GRACE_DAYS:
        return "due"
    return "overdue"


def _last_run_lookup(db: Session, zone_ids: list[uuid.UUID]) -> dict[uuid.UUID, datetime]:
    # Deferred import: app.irrigation.service imports this module at top
    # level (to validate zones via get_zone), so importing it back at this
    # module's top level would be a circular import. This is an intentional,
    # narrow exception to Core not depending on other modules — Zone.status
    # inherently needs Irrigation's CalculationRun data (see CLAUDE.md).
    from app.irrigation import service as irrigation_service

    return irrigation_service.get_last_run_at_by_zone(db, zone_ids)


def _zone_row_to_read_kwargs(zone: Zone, field_name: str, last_run_at: datetime | None) -> dict:
    return {
        "id": zone.id,
        "field_id": zone.field_id,
        "field_name": field_name,
        "name": zone.name,
        "area_ha": zone.area_ha,
        "crop": zone.crop,
        "irrigation_system_type": zone.irrigation_system_type,
        "irrigation_interval_days": zone.irrigation_interval_days,
        "is_active": zone.is_active,
        "status": _compute_zone_status(zone.irrigation_interval_days, last_run_at or zone.created_at),
        "created_at": zone.created_at,
    }


def list_zones(db: Session, field_id: uuid.UUID | None = None, include_inactive: bool = False) -> list[dict]:
    query = db.query(Zone, Field.name).join(Field, Zone.field_id == Field.id)
    if field_id is not None:
        query = query.filter(Zone.field_id == field_id)
    if not include_inactive:
        query = query.filter(Zone.is_active.is_(True))
    rows = query.order_by(Zone.name).all()
    last_run_lookup = _last_run_lookup(db, [zone.id for zone, _ in rows])
    return [_zone_row_to_read_kwargs(zone, field_name, last_run_lookup.get(zone.id)) for zone, field_name in rows]


def get_zone(db: Session, zone_id: uuid.UUID) -> dict | None:
    row = (
        db.query(Zone, Field.name)
        .join(Field, Zone.field_id == Field.id)
        .filter(Zone.id == zone_id)
        .first()
    )
    if row is None:
        return None
    zone, field_name = row
    last_run_at = _last_run_lookup(db, [zone.id]).get(zone.id)
    return _zone_row_to_read_kwargs(zone, field_name, last_run_at)


def create_zone(db: Session, payload: ZoneCreate) -> dict:
    zone = Zone(
        field_id=payload.field_id,
        name=payload.name,
        area_ha=payload.area_ha,
        crop=payload.crop,
        irrigation_system_type=payload.irrigation_system_type,
        irrigation_interval_days=payload.irrigation_interval_days,
    )
    db.add(zone)
    db.commit()
    db.refresh(zone)
    field = db.query(Field).filter(Field.id == zone.field_id).first()
    return _zone_row_to_read_kwargs(zone, field.name if field else "", None)


def update_zone(db: Session, zone: Zone, payload: ZoneUpdate) -> dict:
    for key, value in payload.model_dump(exclude_unset=True).items():
        setattr(zone, key, value)
    db.commit()
    db.refresh(zone)
    field = db.query(Field).filter(Field.id == zone.field_id).first()
    last_run_at = _last_run_lookup(db, [zone.id]).get(zone.id)
    return _zone_row_to_read_kwargs(zone, field.name if field else "", last_run_at)


def archive_zone(db: Session, zone: Zone) -> dict:
    zone.is_active = False
    db.commit()
    db.refresh(zone)
    field = db.query(Field).filter(Field.id == zone.field_id).first()
    last_run_at = _last_run_lookup(db, [zone.id]).get(zone.id)
    return _zone_row_to_read_kwargs(zone, field.name if field else "", last_run_at)
