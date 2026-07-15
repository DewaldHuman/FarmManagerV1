import uuid
from datetime import datetime
from typing import Literal

from pydantic import BaseModel

from app.core.models import IrrigationSystemType, Role


class HealthResponse(BaseModel):
    status: str


class Token(BaseModel):
    access_token: str
    token_type: str = "bearer"


class UserRead(BaseModel):
    id: uuid.UUID
    username: str
    display_name: str
    role: Role
    is_active: bool
    preferred_language: str

    model_config = {"from_attributes": True}


class LanguagePreferenceUpdate(BaseModel):
    language: Literal["en", "af"]


class SettingsRead(BaseModel):
    id: uuid.UUID
    timezone: str
    kc_establishment: float
    kc_midseason: float
    kc_lateseason: float

    model_config = {"from_attributes": True}


class SettingsUpdate(BaseModel):
    timezone: str | None = None
    kc_establishment: float | None = None
    kc_midseason: float | None = None
    kc_lateseason: float | None = None


class FarmRead(BaseModel):
    id: uuid.UUID
    name: str

    model_config = {"from_attributes": True}


class FieldCreate(BaseModel):
    name: str
    area_ha: float
    notes: str | None = None


class FieldUpdate(BaseModel):
    name: str | None = None
    area_ha: float | None = None
    notes: str | None = None


class FieldRead(BaseModel):
    id: uuid.UUID
    farm_id: uuid.UUID
    name: str
    area_ha: float
    notes: str | None
    is_active: bool
    created_at: datetime

    model_config = {"from_attributes": True}


class ZoneCreate(BaseModel):
    field_id: uuid.UUID
    name: str
    area_ha: float
    crop: str | None = None
    irrigation_system_type: IrrigationSystemType = IrrigationSystemType.NONE
    irrigation_interval_days: int | None = None


class ZoneUpdate(BaseModel):
    field_id: uuid.UUID | None = None
    name: str | None = None
    area_ha: float | None = None
    crop: str | None = None
    irrigation_system_type: IrrigationSystemType | None = None
    irrigation_interval_days: int | None = None


class ZoneRead(BaseModel):
    id: uuid.UUID
    field_id: uuid.UUID
    field_name: str
    name: str
    area_ha: float
    crop: str | None
    irrigation_system_type: IrrigationSystemType
    irrigation_interval_days: int | None
    is_active: bool
    status: str
    created_at: datetime

    model_config = {"from_attributes": True}
