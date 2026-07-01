import { TestBed } from '@angular/core/testing';
import { BoardComponent } from './board.component';

describe('BoardComponent', () => {
  it('renders the board placeholder', () => {
    TestBed.configureTestingModule({ imports: [BoardComponent] });
    const fixture = TestBed.createComponent(BoardComponent);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Board');
  });
});
