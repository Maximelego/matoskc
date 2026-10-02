import type { InspectionAnswer, InspectionQuestion, InspectionSection } from "../../api/inspections/dto";

export function emptyAnswer(question: InspectionQuestion): InspectionAnswer {
  switch (question.kind) {
    case "condition": return { questionId: question.id, kind: "condition", value: null, observation: "", photos: [] };
    case "text": return { questionId: question.id, kind: "text", value: "" };
    case "number": return { questionId: question.id, kind: "number", value: null };
  }
}

export function answerErrors(question: InspectionQuestion, answer: InspectionAnswer | undefined): string[] {
  if (!answer || answer.kind !== question.kind || answer.questionId !== question.id) return ["Réponse manquante."];
  switch (question.kind) {
    case "condition": {
      if (answer.kind !== "condition") return ["Réponse invalide."];
      const errors: string[] = [];
      if (question.required && answer.value === null) errors.push("Veuillez indiquer l’état de cet élément.");
      if (answer.value === "nonCompliant" && !answer.observation.trim()) errors.push("Décrivez la non-conformité.");
      const requiresPhoto = question.photoRequiredWhen === "always" ||
        (question.photoRequiredWhen === "nonCompliant" && answer.value === "nonCompliant");
      if (requiresPhoto && answer.photos.length === 0) errors.push("Ajoutez une photographie.");
      return errors;
    }
    case "text": {
      if (answer.kind !== "text") return ["Réponse invalide."];
      if (question.required && !answer.value.trim()) return ["Veuillez renseigner ce champ."];
      if (question.maxLength && answer.value.length > question.maxLength) return [`Limite de ${question.maxLength} caractères dépassée.`];
      return [];
    }
    case "number": {
      if (answer.kind !== "number") return ["Réponse invalide."];
      if (answer.value === null) return question.required ? ["Veuillez saisir une valeur."] : [];
      if (!Number.isFinite(answer.value)) return ["Valeur invalide."];
      if (question.min !== undefined && answer.value < question.min) return [`Valeur minimale : ${question.min}.`];
      if (question.max !== undefined && answer.value > question.max) return [`Valeur maximale : ${question.max}.`];
      return [];
    }
  }
}

export function sectionErrors(section: InspectionSection, answers: Record<string, InspectionAnswer>): Record<string, string[]> {
  return Object.fromEntries(section.questions.map((question) => [question.id, answerErrors(question, answers[question.id])])) as Record<string, string[]>;
}
