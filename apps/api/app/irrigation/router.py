import uuid
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.auth import get_current_user
from app.core.models import User
from app.db import get_db
from app.irrigation import service
from app.irrigation.schemas import (
    CalculationRunCreate,
    CalculationRunRead,
    ZoneDesignVersionCreate,
    ZoneDesignVersionInfo,
    ZoneDesignVersionRead,
)

router = APIRouter(prefix="/api/v1/irrigation", tags=["irrigation"])


@router.post("/calculation-runs", response_model=CalculationRunRead, status_code=status.HTTP_201_CREATED)
def create_calculation_run(
    payload: CalculationRunCreate,
    db: Annotated[Session, Depends(get_db)],
    current_user: Annotated[User, Depends(get_current_user)],
) -> dict:
    try:
        return service.create_calculation_run(db, payload, created_by=current_user.id)
    except ValueError:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")


@router.get("/calculation-runs", response_model=list[CalculationRunRead])
def list_calculation_runs(
    zone_id: uuid.UUID,
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
) -> list[dict]:
    try:
        return service.list_calculation_runs(db, zone_id)
    except ValueError:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")


@router.post(
    "/zones/{zone_id}/designs", response_model=ZoneDesignVersionRead, status_code=status.HTTP_201_CREATED
)
def create_design_version(
    zone_id: uuid.UUID,
    payload: ZoneDesignVersionCreate,
    db: Annotated[Session, Depends(get_db)],
    current_user: Annotated[User, Depends(get_current_user)],
):
    try:
        return service.create_design_version(db, zone_id, payload, created_by=current_user.id)
    except ValueError:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")


@router.get("/zones/{zone_id}/designs", response_model=list[ZoneDesignVersionInfo])
def list_design_versions(
    zone_id: uuid.UUID,
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
):
    try:
        return service.list_design_versions(db, zone_id)
    except ValueError:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")


@router.get("/zones/{zone_id}/designs/latest", response_model=ZoneDesignVersionRead | None)
def get_latest_design(
    zone_id: uuid.UUID,
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
):
    # 200 with a null body when no design exists — "no design yet" is a normal
    # state for the client, not an error.
    try:
        return service.get_latest_design(db, zone_id)
    except ValueError:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")


@router.get("/designs/{design_id}", response_model=ZoneDesignVersionRead)
def get_design_version(
    design_id: uuid.UUID,
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
):
    version = service.get_design_version(db, design_id)
    if version is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Design not found")
    return version
