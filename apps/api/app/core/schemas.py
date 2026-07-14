import uuid

from pydantic import BaseModel

from app.core.models import Role


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

    model_config = {"from_attributes": True}
