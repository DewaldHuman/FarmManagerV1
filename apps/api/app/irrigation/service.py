import uuid
from datetime import datetime

from sqlalchemy import func
from sqlalchemy.orm import Session

from app.core import service as core_service
from app.irrigation.models import CalculationRun, ZoneDesignVersion
from app.irrigation.schemas import CalculationRunCreate, ZoneDesignVersionCreate


def _run_to_read_kwargs(run: CalculationRun, zone_name: str) -> dict:
    return {
        "id": run.id,
        "zone_id": run.zone_id,
        "zone_name": zone_name,
        "calculator_type": run.calculator_type,
        "inputs": run.inputs,
        "outputs": run.outputs,
        "created_at": run.created_at,
    }


def create_calculation_run(db: Session, payload: CalculationRunCreate, created_by: uuid.UUID) -> dict:
    zone = core_service.get_zone(db, payload.zone_id)
    if zone is None:
        raise ValueError("Zone not found")

    run = CalculationRun(
        zone_id=payload.zone_id,
        calculator_type=payload.calculator_type,
        inputs=payload.inputs,
        outputs=payload.outputs,
        created_by=created_by,
    )
    db.add(run)
    db.commit()
    db.refresh(run)
    return _run_to_read_kwargs(run, zone["name"])


def list_calculation_runs(db: Session, zone_id: uuid.UUID) -> list[dict]:
    zone = core_service.get_zone(db, zone_id)
    if zone is None:
        raise ValueError("Zone not found")

    runs = (
        db.query(CalculationRun)
        .filter(CalculationRun.zone_id == zone_id)
        .order_by(CalculationRun.created_at.desc())
        .all()
    )
    return [_run_to_read_kwargs(run, zone["name"]) for run in runs]


def create_design_version(
    db: Session, zone_id: uuid.UUID, payload: ZoneDesignVersionCreate, created_by: uuid.UUID
) -> ZoneDesignVersion:
    zone = core_service.get_zone(db, zone_id)
    if zone is None:
        raise ValueError("Zone not found")

    version = ZoneDesignVersion(
        zone_id=zone_id,
        name=payload.name,
        design=payload.design,
        created_by=created_by,
    )
    db.add(version)
    db.commit()
    db.refresh(version)
    return version


def list_design_versions(db: Session, zone_id: uuid.UUID) -> list[ZoneDesignVersion]:
    zone = core_service.get_zone(db, zone_id)
    if zone is None:
        raise ValueError("Zone not found")

    return (
        db.query(ZoneDesignVersion)
        .filter(ZoneDesignVersion.zone_id == zone_id)
        .order_by(ZoneDesignVersion.created_at.desc())
        .all()
    )


def get_latest_design(db: Session, zone_id: uuid.UUID) -> ZoneDesignVersion | None:
    zone = core_service.get_zone(db, zone_id)
    if zone is None:
        raise ValueError("Zone not found")

    return (
        db.query(ZoneDesignVersion)
        .filter(ZoneDesignVersion.zone_id == zone_id)
        .order_by(ZoneDesignVersion.created_at.desc())
        .first()
    )


def get_design_version(db: Session, design_id: uuid.UUID) -> ZoneDesignVersion | None:
    return db.query(ZoneDesignVersion).filter(ZoneDesignVersion.id == design_id).first()


def get_last_run_at_by_zone(db: Session, zone_ids: list[uuid.UUID]) -> dict[uuid.UUID, datetime]:
    """Most recent CalculationRun.created_at per zone id — consumed by
    app.core.service to compute Zone.status (on-schedule/due/overdue).
    """
    if not zone_ids:
        return {}

    rows = (
        db.query(CalculationRun.zone_id, func.max(CalculationRun.created_at))
        .filter(CalculationRun.zone_id.in_(zone_ids))
        .group_by(CalculationRun.zone_id)
        .all()
    )
    return dict(rows)
