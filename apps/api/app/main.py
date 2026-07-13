from fastapi import FastAPI

from app.core.router import router as core_router

app = FastAPI(title="Farm Platform API")

app.include_router(core_router)
