/** Epic shapes mirroring the backend /api/teams/{teamId}/epics + /api/epics contract. */

export interface Epic {
  id: string;
  teamId: string;
  title: string;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
  ticketCount: number;
}

/** Request body for creating or updating an epic (team is fixed at creation). */
export interface SaveEpicRequest {
  title: string;
  description: string | null;
}
