import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Placeholder for the future Kanban board. The toolbar/nav and logout now live
 * in MainLayoutComponent, which hosts this route.
 */
@Component({
  selector: 'app-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Board</h1>
    <p>The Kanban board is coming soon. You are signed in.</p>
  `,
})
export class BoardComponent {}
