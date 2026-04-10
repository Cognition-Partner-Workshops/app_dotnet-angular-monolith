export interface RfpResponse {
  id: number;
  fileName: string;
  clientName: string;
  status: string;
  sections: RfpResponseSection[];
}

export interface RfpResponseSection {
  sectionNumber: number;
  sectionTitle: string;
  content: string;
  status: string;
  generatedAt: string;
  regeneratedAt: string | null;
}
