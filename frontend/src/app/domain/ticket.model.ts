/** Ticket shapes mirroring the backend /api/teams/{teamId}/tickets contract. */

export type TicketState =
  | 'new'
  | 'ready_for_implementation'
  | 'in_progress'
  | 'ready_for_acceptance'
  | 'done';

export type TicketType = 'bug' | 'feature' | 'fix';

/** A ticket as shown on the Kanban board. */
export interface TicketSummary {
  id: string;
  teamId: string;
  type: TicketType;
  state: TicketState;
  title: string;
  epicId: string | null;
  epicTitle: string | null;
  createdAt: string;
  modifiedAt: string;
}

/** A single ticket with its full body (create/update/get-by-id responses). */
export interface TicketDetail extends TicketSummary {
  body: string;
  createdById: string;
}

/** Request body for creating or updating a ticket (team fixed at creation). */
export interface SaveTicketRequest {
  type: TicketType;
  title: string;
  body: string;
  epicId: string | null;
}

/** Request body for moving a ticket to a new workflow state. */
export interface ChangeTicketStateRequest {
  state: TicketState;
}

/** The five fixed board columns, in workflow order. */
export const BOARD_COLUMNS: readonly { state: TicketState; label: string }[] = [
  { state: 'new', label: 'New' },
  { state: 'ready_for_implementation', label: 'Ready for implementation' },
  { state: 'in_progress', label: 'In progress' },
  { state: 'ready_for_acceptance', label: 'Ready for acceptance' },
  { state: 'done', label: 'Done' },
];

/** Ticket types for the board's Type filter. */
export const TICKET_TYPES: readonly { value: TicketType; label: string }[] = [
  { value: 'bug', label: 'Bug' },
  { value: 'feature', label: 'Feature' },
  { value: 'fix', label: 'Fix' },
];
