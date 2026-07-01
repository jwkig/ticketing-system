/** Team shapes mirroring the backend /api/teams contract. */

export interface Team {
  id: string;
  name: string;
  createdAt: string;
  modifiedAt: string;
  ticketCount: number;
  epicCount: number;
}

/** Request body for creating or renaming a team. */
export interface TeamNameRequest {
  name: string;
}
