import enum
import uuid
from datetime import datetime, timezone

from sqlalchemy import DateTime, Enum
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
