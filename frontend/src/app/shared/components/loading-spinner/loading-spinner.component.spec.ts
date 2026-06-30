import { TestBed } from '@angular/core/testing';
import { LoadingSpinnerComponent } from './loading-spinner.component';

describe('LoadingSpinnerComponent', () => {
  it('renders the label when one is provided', () => {
    TestBed.configureTestingModule({ imports: [LoadingSpinnerComponent] });
    const fixture = TestBed.createComponent(LoadingSpinnerComponent);
    fixture.componentRef.setInput('label', 'Loading…');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading…');
  });
});
