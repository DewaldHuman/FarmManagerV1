from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.core.router import router as core_router
from app.irrigation.router import router as irrigation_router

app = FastAPI(title="Farm Platform API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5083",
        "https://localhost:7157",
        "http://localhost:5257",  # Claude Code browser-preview tooling (.claude/launch.json)
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(core_router)
app.include_router(irrigation_router)
