import enum
import uuid
from datetime import datetime, timezone

from sqlalchemy import Boolean, DateTime, Enum, Float, ForeignKey, String
from sqlalchemy.orm import Mapped, mapped_column

from app.db import Base


class CoreSchemaBase(Base):
    __abstract__ = True
    __table_args__ = {"schema": "core"}


class Role(str, enum.Enum):
    OWNER = "owner"
    MANAGER = "manager"
    WORKER = "worker"


class User(CoreSchemaBase):
    __tablename__ = "users"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    username: Mapped[str] = mapped_column(String(64), unique=True, index=True, nullable=False)
    display_name: Mapped[str] = mapped_column(String(128), nullable=False)
    hashed_password: Mapped[str] = mapped_column(String(255), nullable=False)
    role: Mapped[Role] = mapped_column(
        Enum(Role, native_enum=False, length=16, values_callable=lambda enum_cls: [member.value for member in enum_cls]),
        nullable=False,
    )
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    preferred_language: Mapped[str] = mapped_column(String(8), nullable=False, default="en", server_default="en")
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )


class IrrigationSystemType(str, enum.Enum):
    DRIP = "drip"
    SPRINKLER = "sprinkler"
    FLOOD = "flood"
    NONE = "none"


class Farm(CoreSchemaBase):
    __tablename__ = "farms"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    name: Mapped[str] = mapped_column(String(128), nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )


class Field(CoreSchemaBase):
    __tablename__ = "fields"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    farm_id: Mapped[uuid.UUID] = mapped_column(ForeignKey("core.farms.id"), nullable=False, index=True)
    name: Mapped[str] = mapped_column(String(128), nullable=False)
    area_ha: Mapped[float] = mapped_column(Float, nullable=False)
    # Note: typed as Mapped[str] (not Mapped[str | None]) despite nullable=True —
    # SQLAlchemy 2.0.36 on Python 3.14 fails to de-stringify PEP 604 union
    # annotations in declarative_scan (a real environment bug, not a modeling
    # choice). Nullability is still fully enforced by nullable=True below.
    notes: Mapped[str] = mapped_column(String(1024), nullable=True)
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )


class Settings(CoreSchemaBase):
    __tablename__ = "settings"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    timezone: Mapped[str] = mapped_column(String(64), nullable=False, default="Africa/Johannesburg")
    # Defaults match Farm.Irrigation.Calculators/Presets.cs's CropCoefficients —
    # this is the seam that lets that C# file's hardcoded placeholders eventually
    # be swapped for farm-specific values (plan.md).
    kc_establishment: Mapped[float] = mapped_column(Float, nullable=False, default=0.50)
    kc_midseason: Mapped[float] = mapped_column(Float, nullable=False, default=1.05)
    kc_lateseason: Mapped[float] = mapped_column(Float, nullable=False, default=0.80)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )


class Zone(CoreSchemaBase):
    __tablename__ = "zones"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid4)
    field_id: Mapped[uuid.UUID] = mapped_column(ForeignKey("core.fields.id"), nullable=False, index=True)
    name: Mapped[str] = mapped_column(String(128), nullable=False)
    area_ha: Mapped[float] = mapped_column(Float, nullable=False)
    # See Field.notes above re: Mapped[str] instead of Mapped[str | None] here.
    crop: Mapped[str] = mapped_column(String(128), nullable=True)
    irrigation_system_type: Mapped[IrrigationSystemType] = mapped_column(
        Enum(
            IrrigationSystemType,
            native_enum=False,
            length=16,
            values_callable=lambda enum_cls: [member.value for member in enum_cls],
        ),
        nullable=False,
        default=IrrigationSystemType.NONE,
    )
    is_active: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), nullable=False
    )
