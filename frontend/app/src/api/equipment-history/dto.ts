export type EquipmentAuditChangeDto = { field: string; before: string | null; after: string | null };
export type EquipmentAuditEntryDto = {
  id: string;
  entityType: string;
  entityId: string;
  operation: string;
  accountId: string | null;
  occurredAt: string;
  reason: string | null;
  changesJson: string;
};
