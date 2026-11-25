import { TestBed } from '@angular/core/testing';
import { I18nService } from '../i18n/i18n.service';
import { LibraryEditor } from './library-editor';

describe('LibraryEditor', () => {
    afterEach(() => {
        localStorage.removeItem('pathfinder-elements.language');
    });

    it('preserves the draft and translates the editor while editing', () => {
        const fixture = TestBed.createComponent(LibraryEditor);
        const i18n = TestBed.inject(I18nService);
        i18n.setLanguage('en');
        fixture.componentRef.setInput('selection', { kind: 'monsters' });
        fixture.detectChanges();
        const editor = fixture.componentInstance;
        editor.form().controls.name.setValue('Goblin A');
        const draft = editor.form();
        const page: HTMLElement = fixture.nativeElement;
        expect(page.querySelector('h2')?.textContent).toBe('New monster');

        i18n.setLanguage('de');
        fixture.detectChanges();
        expect(editor.form()).toBe(draft);
        expect(page.querySelector('h2')?.textContent).toBe('Neues Monster');
        expect(page.querySelector<HTMLInputElement>('input[formControlName="name"]')?.value).toBe('Goblin A');
    });

    it('shows invalid required values and keeps them out of the save request', () => {
        const fixture = TestBed.createComponent(LibraryEditor);
        const i18n = TestBed.inject(I18nService);
        i18n.setLanguage('de');
        fixture.componentRef.setInput('selection', { kind: 'characters' });
        fixture.detectChanges();
        const editor = fixture.componentInstance;
        const submitted = vi.fn();
        editor.saveTemplate.subscribe(submitted);
        editor.form().controls.name.setValue(' ');
        editor.form().controls.statistics.controls.maxHitPoints.setValue(0);
        editor.submit();
        fixture.detectChanges();
        expect(submitted).not.toHaveBeenCalled();
        const page: HTMLElement = fixture.nativeElement;
        expect(page.querySelector('[role="alert"]')?.textContent).toContain('Maximale TP');
        const hpField = [...page.querySelectorAll('label.form-field')].find((label) => label.textContent.includes('Maximale TP'));
        expect(hpField?.querySelector('input')?.getAttribute('aria-invalid')).toBe('true');
    });
});
