from fastapi import APIRouter

from app.core import service
from app.core.schemas import HealthResponse

router = APIRouter(prefix="/api/v1/core", tags=["core"])


@router.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(status=service.get_health_status())
