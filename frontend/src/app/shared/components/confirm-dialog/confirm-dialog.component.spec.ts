import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';

describe('ConfirmDialogComponent', () => {
  function setup(data: ConfirmDialogData) {
    TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent],
      providers: [{ provide: MAT_DIALOG_DATA, useValue: data }],
    });
    const fixture = TestBed.createComponent(ConfirmDialogComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('renders the title and message', () => {
    const fixture = setup({ title: 'Delete team', message: 'Delete "X"?' });
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Delete team');
    expect(text).toContain('Delete "X"?');
  });

  it('uses custom button labels when provided', () => {
    const fixture = setup({ title: 'T', message: 'M', confirmLabel: 'Remove', cancelLabel: 'Keep' });
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Remove');
    expect(text).toContain('Keep');
  });
});
