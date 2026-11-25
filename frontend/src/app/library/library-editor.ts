import { ChangeDetectionStrategy, Component, computed, effect, type ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { I18nService } from '../i18n/i18n.service';
import { issueDescription } from '../i18n/library-messages';
import { type EditorSelection, statisticFields, type TemplateWrite } from './library-model';
import { createAttackForm, createDamageForm, createSequenceForm, createTemplateForm, formIssues, moveFormItem } from './template-form';

@Component({
    selector: 'app-library-editor',
    imports: [ReactiveFormsModule],
    templateUrl: './library-editor.html',
    styleUrl: './library-editor.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LibraryEditor {
    private readonly i18n = inject(I18nService);

    readonly selection = input.required<EditorSelection>();
    readonly busy = input(false);
    readonly saveTemplate = output<TemplateWrite>();
    readonly setArchived = output<boolean>();
    readonly deleteTemplate = output<void>();
    readonly closeEditor = output<void>();

    readonly copy = computed(() => this.i18n.messages().library);
    readonly form = computed(() => createTemplateForm(this.selection().saved?.template));
    private readonly headingElement = viewChild<ElementRef<HTMLHeadingElement>>('editorHeading');
    readonly saved = computed(() => this.selection().saved?.template);
    readonly heading = computed(() => {
        const copy = this.copy();
        if (this.selection().kind === 'characters') {
            return this.saved() ? copy.editCharacter : copy.newCharacter;
        }
        return this.saved() ? copy.editMonster : copy.newMonster;
    });
    // Keep validation and confirmation tied to the currently open form.
    private readonly checkedForm = signal<ReturnType<typeof createTemplateForm> | null>(null);
    private readonly formToDelete = signal<ReturnType<typeof createTemplateForm> | null>(null);
    readonly confirmingDelete = computed(() => this.formToDelete() === this.form());
    readonly statisticFields = statisticFields;
    readonly attackNumbers = [
        { name: 'attackBonus', min: null, max: null },
        { name: 'criticalThreatMinimum', min: 2, max: 20 },
        { name: 'criticalMultiplier', min: 2, max: 10 }
    ] as const;
    readonly damageNumbers = [
        { name: 'diceCount', min: 0, max: 100 },
        { name: 'dieSides', min: 0, max: 100 },
        { name: 'modifier', min: null, max: null }
    ] as const;
    readonly createSequenceForm = createSequenceForm;
    readonly createAttackForm = createAttackForm;
    readonly createDamageForm = createDamageForm;
    readonly moveFormItem = moveFormItem;

    constructor() {
        effect(() => {
            this.selection();
            this.headingElement()?.nativeElement.focus();
        });
    }

    validationMessages(): string[] {
        const form = this.form();
        if (this.checkedForm() !== form || form.valid) {
            return [];
        }
        const { fields, errors } = this.copy();
        return formIssues(form).map(({ path, code }) => issueDescription(fields, errors, path, code));
    }

    submit(): void {
        const form = this.form();
        this.checkedForm.set(form);
        form.markAllAsTouched();
        if (this.busy() || form.invalid) {
            return;
        }

        // The form validators require all numbers before the draft can be sent.
        const { name, statistics, attackSequences } = form.getRawValue() as Required<TemplateWrite>;
        this.saveTemplate.emit(this.selection().kind === 'monsters' ? { name, statistics, attackSequences } : { name, statistics });
    }

    requestDelete(): void {
        this.formToDelete.set(this.form());
    }

    cancelDelete(): void {
        this.formToDelete.set(null);
    }

    confirmDelete(): void {
        if (!this.busy() && this.confirmingDelete()) {
            this.cancelDelete();
            this.deleteTemplate.emit();
        }
    }
}
