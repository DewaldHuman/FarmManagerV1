from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    database_url: str = "postgresql+psycopg://farm:farm@localhost:5432/farm_platform"
    jwt_secret: str = "change_me_too"
    jwt_expiry_minutes: int = 60


settings = Settings()
