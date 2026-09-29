"""
This module defines the EquipmentCategory entity, which represents a
category of equipment in the system.
"""

class EquipmentCategory:
    """
    Represents a category of equipment in the system.
    Maybe for instance a forklift, a pallet truck, a hand truck, etc.
    """
    def __init__(self, name: str, description: str):
        """
        Initializes an EquipmentCategory instance.

        Args:
            name (str): The name of the equipment category.
            description (str): The description of the equipment category.
        """
        self.name = name
        self.description = description

    def __repr__(self):
        """
        Returns a string representation of the EquipmentCategory instance.
        """
        return f"EquipmentCategory(name={self.name}, description={self.description})"
