"""
Equipment Entity Module
This module defines the Equipment entity, which represents an equipment item in the system.
"""

from uuid import UUID

from app.domain.equipment.entities.base_entity import BaseEntity
from app.domain.equipment.entities.equipment_category import EquipmentCategory
from app.domain.equipment.enums.equipment_status import ALLOWED_STATUS_TRANSITIONS, EquipmentStatus


class Equipment(BaseEntity):
    """
    Represents an equipment item in the system.
    """
    def __init__(self, name: str, category: EquipmentCategory, serial_number: str, uuid: UUID | None = None):
        """
        Initializes an Equipment instance.

        Args:
            uuid (UUID | None): The unique identifier for the equipment.
            name (str): The name of the equipment.
            category (EquipmentCategory): The category of the equipment.
            serial_number (str): The serial number of the equipment.
        """
        super().__init__(uuid=uuid)

        self.name = name
        self.category = category
        self.serial_number = serial_number
        self.status = EquipmentStatus.AVAILABLE


    def __can_transition_to(self, target_status: EquipmentStatus) -> bool:
        """
        Checks if the equipment can transition to a new status.

        Args:
            target_status (EquipmentStatus): The new status to check.

        Returns:
            bool: True if the transition is allowed, False otherwise.
        """
        return target_status in ALLOWED_STATUS_TRANSITIONS[self.status]


    def transition_to(self, new_status: EquipmentStatus):
        """
        Transitions the equipment to a new status.

        Args:
            new_status (EquipmentStatus): The new status to transition to.
        """
        if not self.__can_transition_to(new_status):
            raise ValueError(f"Transition from {self.status} to {new_status} is not allowed.")
        self.status = new_status
