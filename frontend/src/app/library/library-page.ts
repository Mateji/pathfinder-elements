import { ChangeDetectionStrategy, Component, computed, DOCUMENT, effect, inject, type OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { I18nService } from '../i18n/i18n.service';
import { issueDescription } from '../i18n/library-messages';
import { LibraryEditor } from './library-editor';
import { LibraryStore } from './library-store';

@Component({
    selector: 'app-library-page',
    imports: [ReactiveFormsModule, LibraryEditor],
    providers: [LibraryStore],
    templateUrl: './library-page.html',
    styleUrl: './library-page.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LibraryPage implements OnInit {
    private readonly i18n = inject(I18nService);

    readonly store = inject(LibraryStore);
    readonly secureOrigin = inject(DOCUMENT).location?.protocol === 'https:';
    readonly copy = computed(() => this.i18n.messages().library);
    readonly selectedFile = signal<File | null>(null);
    readonly loginForm = new FormGroup({
        username: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(256)] }),
        password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(256)] })
    });
    readonly feedbackTitle = computed(() => {
        const feedback = this.store.feedback();
        if (!feedback) {
            return null;
        }
        const copy = this.copy();
        return feedback.code === 'imported'
            ? copy.imported(feedback.characters, feedback.monsters)
            : copy.feedback[feedback.code];
    });
    readonly feedbackIssues = computed(() => {
        const feedback = this.store.feedback();
        if (!feedback || !('issues' in feedback) || !feedback.issues) {
            return [];
        }
        const { fields, errors } = this.copy();
        return feedback.issues.map(({ path, code }) => issueDescription(fields, errors, path, code));
    });
    readonly success = computed(() => {
        const code = this.store.feedback()?.code;
        return code === 'saved' || code === 'archived' || code === 'restored' || code === 'deleted' || code === 'imported';
    });

    constructor() {
        effect(() => {
            if (this.store.session().state !== 'signedIn') {
                this.selectedFile.set(null);
            }
        });
    }

    ngOnInit(): void {
        if (this.secureOrigin) {
            void this.store.checkSession();
        }
    }

    signIn(): void {
        if (!this.secureOrigin || this.loginForm.invalid || this.store.busy()) {
            return;
        }
        const { username, password } = this.loginForm.getRawValue();
        this.loginForm.controls.password.reset();
        void this.store.login(username, password);
    }

    selectFile(input: HTMLInputElement): void {
        this.selectedFile.set(input.files?.item(0) ?? null);
    }

    async importFile(input: HTMLInputElement): Promise<void> {
        const file = this.selectedFile();
        if (file && await this.store.importFile(file)) {
            input.value = '';
            this.selectedFile.set(null);
        }
    }

    signedModifier(value: number): string {
        return value > 0 ? `+${value}` : String(value);
    }
}
