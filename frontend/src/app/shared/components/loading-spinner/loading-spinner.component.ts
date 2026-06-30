import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-spinner',
  imports: [MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="loading">
      <mat-progress-spinner mode="indeterminate" [diameter]="diameter()" />
      @if (label()) {
        <p>{{ label() }}</p>
      }
    </div>
  `,
  styles: `
    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      padding: 2rem;
    }
  `,
})
export class LoadingSpinnerComponent {
  readonly diameter = input(48);
  readonly label = input('');
}
