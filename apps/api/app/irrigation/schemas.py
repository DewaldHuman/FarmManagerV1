import uuid
from datetime import datetime

from pydantic import BaseModel

from app.irrigation.models import CalculatorType


class CalculationRunCreate(BaseModel):
    zone_id: uuid.UUID
    calculator_type: CalculatorType
    inputs: dict[str, float]
    outputs: dict[str, float]


class CalculationRunRead(BaseModel):
    id: uuid.UUID
    zone_id: uuid.UUID
    zone_name: str
    calculator_type: CalculatorType
    inputs: dict[str, float]
    outputs: dict[str, float]
    created_at: datetime

    model_config = {"from_attributes": True}


class ZoneDesignVersionCreate(BaseModel):
    name: str | None = None
    # The client's serialized ZoneDesign — opaque to the backend.
    design: dict


class ZoneDesignVersionInfo(BaseModel):
    id: uuid.UUID
    zone_id: uuid.UUID
    name: str | None
    created_at: datetime

    model_config = {"from_attributes": True}


class ZoneDesignVersionRead(ZoneDesignVersionInfo):
    design: dict


class ScheduleUpsert(BaseModel):
    available_water_mm_per_metre: float
    root_depth_metres: float
    allowable_depletion_percent: float
    peak_water_use_mm_per_day: float
    # Computed client-side by SchedulingCalculators and stored verbatim.
    interval_days: float
    readily_available_water_mm: float


class ScheduleRead(ScheduleUpsert):
    id: uuid.UUID
    zone_id: uuid.UUID
    created_at: datetime
    updated_at: datetime

    model_config = {"from_attributes": True}
