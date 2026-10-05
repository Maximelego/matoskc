// Public entry point for inspection data. Replace the demo implementation here when the backend is ready.
export { inspectionsApi } from "./mock";
export { adminInspectionsApi } from "./admin";

export type {
  InspectionRecord, InspectionAnswer, InspectionDirection, InspectionDraft, InspectionQuestion,
  InspectionSection, InspectionTemplateDto, PhotoRule, PreviousDefect,
  PreviousInspection,
} from "./dto";
