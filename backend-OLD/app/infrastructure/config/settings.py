"""Application configuration loaded from environment variables."""

from functools import lru_cache

from pydantic import Field, PostgresDsn, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Validated application settings."""

    database_url: PostgresDsn
    database_echo: bool = False
    database_pool_size: int = Field(default=5, ge=1, le=50)
    database_max_overflow: int = Field(default=10, ge=0, le=100)

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
        validate_default=True,
        hide_input_in_errors=True,
    )

    def __init__(self) -> None:
        """Load settings from configured environment sources."""

        super().__init__()

    @field_validator("database_url")
    @classmethod
    def validate_database_driver(
        cls,
        database_url: PostgresDsn,
    ) -> PostgresDsn:
        """Ensure that PostgreSQL uses the expected driver."""

        if not str(database_url).startswith("postgresql+psycopg://"):
            raise ValueError(
                "DATABASE_URL must use the postgresql+psycopg driver"
            )

        return database_url


@lru_cache
def get_settings() -> Settings:
    """Load, validate and cache application settings."""

    return Settings()