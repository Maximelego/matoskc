"""
Declarative base shared by all SQLAlchemy models.
This module defines the base class for SQLAlchemy models, which includes a naming convention for database constraints and indexes.
"""

from sqlalchemy import MetaData
from sqlalchemy.orm import DeclarativeBase


NAMING_CONVENTION: dict[str, str] = {
    "ix": "ix_%(column_0_label)s",
    "uq": "uq_%(table_name)s_%(column_0_name)s",
    "ck": "ck_%(table_name)s_%(constraint_name)s",
    "fk": "fk_%(table_name)s_%(column_0_name)s_%(referred_table_name)s",
    "pk": "pk_%(table_name)s",
}


class Base(DeclarativeBase):
    """Base class for SQLAlchemy persistence models."""

    metadata = MetaData(naming_convention=NAMING_CONVENTION)