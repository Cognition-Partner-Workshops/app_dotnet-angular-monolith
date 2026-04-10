export interface TrackerEntry {
  id: number;
  rfpId: string;
  rfpDocumentId: number | null;
  rfpTitle: string;
  clientName: string;
  crmId: string | null;
  originatorName: string;
  originatorEmail: string;
  receivedDate: string;
  dueDate: string | null;
  status: string;
  assignedTo: string | null;
  priority: string;
  notes: string | null;
  emailSentForMissingCrm: boolean;
  emailSentAt: string | null;
}
