import type { InspectionAnswer, InspectionTemplateDto, PreviousInspection } from "../../api/inspections/dto";
import type { InspectionDto, SaveInspectionAnswerDto } from "../../api/inspections/dto";
import type { InspectionTemplateVersionDto } from "../../api/inspection-templates/dto";

export function toWizardTemplate(version: InspectionTemplateVersionDto): InspectionTemplateDto {
  return {
    id: version.id, version: version.versionNumber, equipmentCategoryId: version.equipmentCategoryId,
    title: `Modèle de vérification · version ${version.versionNumber}`,
    sections: version.steps.slice().sort((a, b) => a.position - b.position).map(step => ({
      id: step.id, title: step.title,
      questions: step.points.slice().sort((a, b) => a.position - b.position).map(point => {
        const base = { id: point.id, label: point.label, help: point.instructions ?? undefined, required: point.isRequired };
        if (point.responseType === "Conformity") return { ...base, kind: "condition" as const,
          photoRequiredWhen: point.photoRequirement === "Always" ? "always" as const :
            point.photoRequirement === "WhenNonCompliant" ? "nonCompliant" as const : "never" as const };
        if (point.responseType === "Number") return { ...base, kind: "number" as const };
        if (point.responseType === "Choice") return { ...base, kind: "choice" as const, choices: point.choices ?? [] };
        return { ...base, kind: "text" as const };
      }),
    })),
  };
}

export function fromServerAnswer(answer: InspectionDto["answers"][number], kind: "condition" | "text" | "number" | "choice"): InspectionAnswer {
  if (kind === "condition") return { questionId: answer.templatePointId, kind,
    value: answer.result === "Compliant" ? "compliant" : answer.result === "NonCompliant" ? "nonCompliant" : null,
    observation: answer.observation ?? "", photos: [] };
  if (kind === "number") return { questionId: answer.templatePointId, kind, value: answer.numberValue };
  if (kind === "choice") return { questionId: answer.templatePointId, kind, value: answer.choiceValue ?? "" };
  return { questionId: answer.templatePointId, kind, value: answer.textValue ?? "" };
}

export function toServerAnswer(answer: InspectionAnswer, photoIds: string[], existingDefectId: string | null): SaveInspectionAnswerDto {
  return { templatePointId: answer.questionId,
    result: answer.kind === "condition" ? answer.value === "compliant" ? "Compliant" : answer.value === "nonCompliant" ? "NonCompliant" : null : null,
    textValue: answer.kind === "text" ? answer.value : null,
    numberValue: answer.kind === "number" ? answer.value : null,
    choiceValue: answer.kind === "choice" ? answer.value : null,
    observation: answer.kind === "condition" && answer.value === "nonCompliant" ? answer.observation : null,
    photoIds, existingDefectId };
}

export function departureDefects(departure: InspectionDto | null): PreviousInspection | null {
  if (!departure) return null;
  return { id: departure.id, performedAt: departure.validatedAt ?? departure.startedAt,
    defects: departure.answers.filter(answer => answer.result === "NonCompliant")
      .map(answer => ({ questionId: answer.templatePointId, observation: answer.observation ?? "", photoUrls: [] })) };
}
