import uuid
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import OAuth2PasswordRequestForm
from sqlalchemy.orm import Session

from app.core import service
from app.core.auth import authenticate_user, create_access_token_for_user, get_current_user, require_role
from app.core.models import Farm, Field, Role, Settings, User, Zone
from app.core.schemas import (
    FarmRead,
    FieldCreate,
    FieldRead,
    FieldUpdate,
    HealthResponse,
    LanguagePreferenceUpdate,
    SettingsRead,
    SettingsUpdate,
    Token,
    UserRead,
    ZoneCreate,
    ZoneRead,
    ZoneUpdate,
)
from app.db import get_db

router = APIRouter(prefix="/api/v1/core", tags=["core"])


@router.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(status=service.get_health_status())


@router.post("/auth/login", response_model=Token)
def login(
    form_data: Annotated[OAuth2PasswordRequestForm, Depends()],
    db: Annotated[Session, Depends(get_db)],
) -> Token:
    user = authenticate_user(db, form_data.username, form_data.password)
    if user is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect username or password",
            headers={"WWW-Authenticate": "Bearer"},
        )
    return Token(access_token=create_access_token_for_user(user))


@router.get("/auth/me", response_model=UserRead)
def me(current_user: Annotated[User, Depends(get_current_user)]) -> User:
    return current_user


@router.patch("/users/me/language", response_model=UserRead)
def update_my_language(
    payload: LanguagePreferenceUpdate,
    current_user: Annotated[User, Depends(get_current_user)],
    db: Annotated[Session, Depends(get_db)],
) -> User:
    return service.update_user_language(db, current_user, payload.language)


@router.get("/settings", response_model=SettingsRead)
def get_settings(
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
) -> Settings:
    return service.get_the_settings(db)


@router.patch(
    "/settings",
    response_model=SettingsRead,
    dependencies=[Depends(require_role(Role.MANAGER, Role.OWNER))],
)
def update_settings(payload: SettingsUpdate, db: Annotated[Session, Depends(get_db)]) -> Settings:
    settings = service.get_the_settings(db)
    return service.update_settings(db, settings, payload)


@router.get("/farm", response_model=FarmRead)
def get_farm(
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
) -> Farm:
    return service.get_the_farm(db)


@router.get("/fields", response_model=list[FieldRead])
def list_fields(
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
) -> list[Field]:
    return service.list_fields(db)


@router.post(
    "/fields",
    response_model=FieldRead,
    status_code=status.HTTP_201_CREATED,
    dependencies=[Depends(require_role(Role.MANAGER, Role.OWNER))],
)
def create_field(payload: FieldCreate, db: Annotated[Session, Depends(get_db)]) -> Field:
    return service.create_field(db, payload)


@router.get("/fields/{field_id}", response_model=FieldRead)
def get_field(
    field_id: uuid.UUID,
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
) -> Field:
    field = service.get_field(db, field_id)
    if field is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Field not found")
    return field


@router.patch(
    "/fields/{field_id}",
    response_model=FieldRead,
    dependencies=[Depends(require_role(Role.MANAGER, Role.OWNER))],
)
def update_field(field_id: uuid.UUID, payload: FieldUpdate, db: Annotated[Session, Depends(get_db)]) -> Field:
    field = service.get_field(db, field_id)
    if field is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Field not found")
    return service.update_field(db, field, payload)


@router.post(
    "/fields/{field_id}/archive",
    response_model=FieldRead,
    dependencies=[Depends(require_role(Role.MANAGER, Role.OWNER))],
)
def archive_field(field_id: uuid.UUID, db: Annotated[Session, Depends(get_db)]) -> Field:
    field = service.get_field(db, field_id)
    if field is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Field not found")
    return service.archive_field(db, field)


@router.get("/zones", response_model=list[ZoneRead])
def list_zones(
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
    field_id: uuid.UUID | None = None,
) -> list[dict]:
    return service.list_zones(db, field_id=field_id)


@router.post(
    "/zones",
    response_model=ZoneRead,
    status_code=status.HTTP_201_CREATED,
    dependencies=[Depends(require_role(Role.MANAGER, Role.OWNER))],
)
def create_zone(payload: ZoneCreate, db: Annotated[Session, Depends(get_db)]) -> dict:
    field = service.get_field(db, payload.field_id)
    if field is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Field not found")
    return service.create_zone(db, payload)


@router.get("/zones/{zone_id}", response_model=ZoneRead)
def get_zone(
    zone_id: uuid.UUID,
    db: Annotated[Session, Depends(get_db)],
    _current_user: Annotated[User, Depends(get_current_user)],
) -> dict:
    zone = service.get_zone(db, zone_id)
    if zone is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")
    return zone


@router.patch(
    "/zones/{zone_id}",
    response_model=ZoneRead,
    dependencies=[Depends(require_role(Role.MANAGER, Role.OWNER))],
)
def update_zone(zone_id: uuid.UUID, payload: ZoneUpdate, db: Annotated[Session, Depends(get_db)]) -> dict:
    zone_orm = db.query(Zone).filter(Zone.id == zone_id).first()
    if zone_orm is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")
    return service.update_zone(db, zone_orm, payload)


@router.post(
    "/zones/{zone_id}/archive",
    response_model=ZoneRead,
    dependencies=[Depends(require_role(Role.MANAGER, Role.OWNER))],
)
def archive_zone(zone_id: uuid.UUID, db: Annotated[Session, Depends(get_db)]) -> dict:
    zone_orm = db.query(Zone).filter(Zone.id == zone_id).first()
    if zone_orm is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Zone not found")
    return service.archive_zone(db, zone_orm)
