"""
Enum for equipment status.
"""

from enum import Enum
from typing import Final


class EquipmentStatus(str, Enum):
    """
    Enum for equipment status.
    This enum defines the possible statuses for equipment in the system.
    Attributes:
        AVAILABLE (str): Indicates that the equipment is available for use.
        UNAVAILABLE (str): Indicates that the equipment is currently unavailable for use.
        BORROWED (str): Indicates that the equipment is currently borrowed by a user.
        TO_BE_DECIDED (str): Indicates that the status of the equipment is yet to be decided.
        MAINTENANCE (str): Indicates that the equipment is currently under maintenance.
        DECOMMISSIONED (str): Indicates that the equipment has been decommissioned and is no longer in use.
    """
    AVAILABLE           = "available"
    UNAVAILABLE         = "unavailable"
    BORROWED            = "borrowed"
    TO_BE_DECIDED       = "to_be_decided"
    MAINTENANCE         = "maintenance"
    DECOMMISSIONED      = "decommissioned"


ALLOWED_STATUS_TRANSITIONS: Final[
    dict[EquipmentStatus, frozenset[EquipmentStatus]]
] = {
    EquipmentStatus.AVAILABLE: frozenset(
        {
            EquipmentStatus.BORROWED,
            EquipmentStatus.DECOMMISSIONED,
        }
    ),
    EquipmentStatus.BORROWED: frozenset(
        {
            EquipmentStatus.AVAILABLE,
            EquipmentStatus.TO_BE_DECIDED,
        }
    ),
    EquipmentStatus.TO_BE_DECIDED: frozenset(
        {
            EquipmentStatus.AVAILABLE,
            EquipmentStatus.UNAVAILABLE,
            EquipmentStatus.MAINTENANCE,
            EquipmentStatus.DECOMMISSIONED,
        }
    ),
    EquipmentStatus.UNAVAILABLE: frozenset(
        {
            EquipmentStatus.AVAILABLE,
            EquipmentStatus.MAINTENANCE,
            EquipmentStatus.DECOMMISSIONED,
        }
    ),
    EquipmentStatus.MAINTENANCE: frozenset(
        {
            EquipmentStatus.TO_BE_DECIDED,
        }
    ),
    EquipmentStatus.DECOMMISSIONED: frozenset(),
}
