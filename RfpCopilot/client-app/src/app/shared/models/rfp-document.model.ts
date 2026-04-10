export interface RfpDocument {
  id: number;
  fileName: string;
  fileSize: number;
  contentType: string;
  extractedText: string;
  uploadedAt: string;
  clientName: string;
  crmId: string | null;
  originatorEmail: string;
  dueDate: string | null;
  priority: string;
  isCloudMigrationInScope: boolean;
  preferredCloudProvider: string | null;
  status: string;
  sectionCount?: number;
}

export interface RfpStatus {
  id: number;
  fileName: string;
  clientName: string;
  status: string;
  uploadedAt: string;
  agentProgress: AgentProgress[];
}

export interface AgentProgress {
  agentName: string;
  status: string;
  startedAt: string;
  completedAt: string | null;
  errorMessage: string | null;
}
