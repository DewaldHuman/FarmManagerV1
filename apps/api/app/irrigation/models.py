import enum
import uuid
from datetime import datetime, timezone

from sqlalchemy import DateTime, Enum, Float, String
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.orm import Mapped, mapped_column

from app.db import Base


class IrrigationSchemaBase(Base):
    __abstract__ = True
    __table_args__ = {"schema": "irrigation"}


class CalculatorType(str, enum.Enum):
    ET0_RUN_TIME = "et0-run-time"
    MANUAL_VOLUME = "manual-volume"
    DRIP_EMITTER = "drip-emitter"
    SET_TIME = "set-time"


class CalculationRun(IrrigationSchemaBase):
    __tablename__ = "calculation_runs"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    # No FK: core.zones lives in a different schema (rule 3) — existence is
    # enforced at the application layer via app.core.service.get_zone.
    zone_id: Mapped[uuid.UUID] = mapped_column(nullable=False, index=True)
    calculator_type: Mapped[CalculatorType] = mapped_column(
        Enum(
            CalculatorType,
            native_enum=False,
            length=32,
            values_callable=lambda enum_cls: [member.value for member in enum_cls],
        ),
        nullable=False,
    )
    # Computed entirely client-side by Farm.Irrigation.Calculators and submitted
    # verbatim — this module is a logging store, not a calculation engine.
    inputs: Mapped[dict] = mapped_column(JSONB, nullable=False)
    outputs: Mapped[dict] = mapped_column(JSONB, nullable=False)
    created_by: Mapped[uuid.UUID] = mapped_column(nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )


class ZoneDesignVersion(IrrigationSchemaBase):
    """One immutable saved version of a zone's irrigation layout. The newest
    version per zone is that zone's "current design"; the frontend edits a
    local working copy and only an explicit Save creates a row (playground
    edits never reach the backend)."""

    __tablename__ = "zone_design_versions"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    # No FK: core.zones lives in a different schema (rule 3) — existence is
    # enforced at the application layer via app.core.service.get_zone.
    zone_id: Mapped[uuid.UUID] = mapped_column(nullable=False, index=True)
    # Optional user label for the save. See core Field.notes re: Mapped[str]
    # instead of Mapped[str | None] (SQLAlchemy 2.0.36 / Python 3.14 gotcha).
    name: Mapped[str] = mapped_column(String(120), nullable=True)
    # Serialized ZoneDesign, produced and consumed entirely by the Blazor
    # client — stored verbatim, never interpreted server-side.
    design: Mapped[dict] = mapped_column(JSONB, nullable=False)
    created_by: Mapped[uuid.UUID] = mapped_column(nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )


class Schedule(IrrigationSchemaBase):
    """A zone's agronomic irrigation schedule (one per zone). Stores the soil +
    crop parameters and the resulting watering interval, which is computed
    client-side by Farm.Irrigation.Calculators.SchedulingCalculators and stored
    verbatim — the backend never recomputes it (no-server-math rule). Core reads
    interval_days to compute Zone.status, superseding the manual
    core.zones.irrigation_interval_days when a schedule exists."""

    __tablename__ = "schedules"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    # No FK (cross-schema, app-validated); unique — one schedule per zone.
    zone_id: Mapped[uuid.UUID] = mapped_column(nullable=False, unique=True, index=True)
    # Agronomic inputs (entered manually for now; weather/soil feeds are future).
    available_water_mm_per_metre: Mapped[float] = mapped_column(Float, nullable=False)
    root_depth_metres: Mapped[float] = mapped_column(Float, nullable=False)
    allowable_depletion_percent: Mapped[float] = mapped_column(Float, nullable=False)
    peak_water_use_mm_per_day: Mapped[float] = mapped_column(Float, nullable=False)
    # Client-computed outputs, stored verbatim.
    interval_days: Mapped[float] = mapped_column(Float, nullable=False)
    readily_available_water_mm: Mapped[float] = mapped_column(Float, nullable=False)
    updated_by: Mapped[uuid.UUID] = mapped_column(nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )
    updated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        onupdate=lambda: datetime.now(timezone.utc),
        nullable=False,
    )
