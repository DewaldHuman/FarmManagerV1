from app.db import Base


class CoreSchemaBase(Base):
    __abstract__ = True
    __table_args__ = {"schema": "core"}
