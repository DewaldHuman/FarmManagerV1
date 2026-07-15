import uuid

from sqlalchemy.orm import Session

from app.core.models import Farm, Field, Settings, User, Zone
from app.core.schemas import FieldCreate, FieldUpdate, SettingsUpdate, ZoneCreate, ZoneUpdate


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


def _zone_row_to_read_kwargs(zone: Zone, field_name: str) -> dict:
    return {
        "id": zone.id,
        "field_id": zone.field_id,
        "field_name": field_name,
        "name": zone.name,
        "area_ha": zone.area_ha,
        "crop": zone.crop,
        "irrigation_system_type": zone.irrigation_system_type,
        "is_active": zone.is_active,
        # TODO(irrigation): replace with real due/overdue computation once
        # Irrigation's schedule + CalculationRun model lands.
        "status": "on-schedule",
        "created_at": zone.created_at,
    }


def list_zones(db: Session, field_id: uuid.UUID | None = None, include_inactive: bool = False) -> list[dict]:
    query = db.query(Zone, Field.name).join(Field, Zone.field_id == Field.id)
    if field_id is not None:
        query = query.filter(Zone.field_id == field_id)
    if not include_inactive:
        query = query.filter(Zone.is_active.is_(True))
    rows = query.order_by(Zone.name).all()
    return [_zone_row_to_read_kwargs(zone, field_name) for zone, field_name in rows]


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
    return _zone_row_to_read_kwargs(zone, field_name)


def create_zone(db: Session, payload: ZoneCreate) -> dict:
    zone = Zone(
        field_id=payload.field_id,
        name=payload.name,
        area_ha=payload.area_ha,
        crop=payload.crop,
        irrigation_system_type=payload.irrigation_system_type,
    )
    db.add(zone)
    db.commit()
    db.refresh(zone)
    field = db.query(Field).filter(Field.id == zone.field_id).first()
    return _zone_row_to_read_kwargs(zone, field.name if field else "")


def update_zone(db: Session, zone: Zone, payload: ZoneUpdate) -> dict:
    for key, value in payload.model_dump(exclude_unset=True).items():
        setattr(zone, key, value)
    db.commit()
    db.refresh(zone)
    field = db.query(Field).filter(Field.id == zone.field_id).first()
    return _zone_row_to_read_kwargs(zone, field.name if field else "")


def archive_zone(db: Session, zone: Zone) -> dict:
    zone.is_active = False
    db.commit()
    db.refresh(zone)
    field = db.query(Field).filter(Field.id == zone.field_id).first()
    return _zone_row_to_read_kwargs(zone, field.name if field else "")
