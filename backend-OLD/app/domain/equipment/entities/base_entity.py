"""
Base class for all entities in the system.
"""

from abc import ABC
from uuid import UUID
import uuid


class BaseEntity(ABC):
    """
    Base class for all entities in the system.
    This class provides a common interface for all entities, allowing them to be
    identified by a unique identifier (uuid).
    """
    def __init__(self, uuid: None | UUID = None, created_at: None=None, updated_at: None=None):
        """
        Initializes a BaseEntity instance.
        
        Args:
            uuid (UUID | None): The unique identifier for the entity.
            created_at: datetime = None: The timestamp for when the entity was created.
            updated_at: datetime = None: The timestamp for when the entity was last updated.
        """
        # If no uuid is provided, generate a new one (when creating a new entity).
        self.uuid = uuid or self.__generate_next_uuid()

        # TODO: Implement created_at and updated_at attributes in the future
        self.created_at = None  # Placeholder for creation timestamp
        self.updated_at = None  # Placeholder for last update timestamp

    def __repr__(self):
        """
        Returns a string representation of the BaseEntity instance.
        Uses Introspection to dynamically include all attributes of
        the instance in the representation.
        """
        class_name: str = self.__class__.__name__
        attributes:str = ', '.join(f"{key}={value}" for key, value in self.__dict__.items())
        return f"{class_name}({attributes})"

    def __generate_next_uuid(self) -> UUID:
        """
        Generates the next unique identifier (uuid) for the entity.
        This method should be implemented by subclasses to provide a mechanism
        for generating unique identifiers.
        """
        return uuid.uuid4()