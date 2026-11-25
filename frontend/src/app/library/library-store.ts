import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LibraryApi } from './library-api';
import { type EditorSelection, type FieldIssue, type LibrarySession, type LibraryTemplate, maximumImportBytes,
    type TemplateKind, type TemplateWrite } from './library-model';

type SessionState = { state: 'checking' | 'signedOut' | 'unavailable' } | { state: 'signedIn'; session: LibrarySession };

type SuccessCode = 'saved' | 'archived' | 'restored' | 'deleted';
type FailureCode = 'unavailable' | 'invalidLogin' | 'sessionExpired' | 'loadFailed' | 'saveFailed' | 'actionFailed'
    | 'stale' | 'referenced' | 'invalidTemplate' | 'invalidImport' | 'duplicateImport' | 'fileTooLarge' | 'importFailed' | 'rateLimited';
export type Feedback =
    | { code: SuccessCode }
    | { code: FailureCode; issues?: FieldIssue[] }
    | { code: 'imported'; characters: number; monsters: number };
type Action = 'session' | 'login' | 'load' | 'save' | 'change' | 'import';

const failureCodes: Record<Action, FailureCode> = {
    session: 'unavailable', login: 'unavailable', load: 'loadFailed', save: 'saveFailed', change: 'actionFailed', import: 'importFailed'
};

@Injectable()
export class LibraryStore {
    private readonly api = inject(LibraryApi);
    private readonly sessionState = signal<SessionState>({ state: 'checking' });
    private readonly selectedKind = signal<TemplateKind>('characters');
    private readonly showArchived = signal(false);
    private readonly currentTemplates = signal<readonly LibraryTemplate[]>([]);
    private readonly currentEditor = signal<EditorSelection | null>(null);
    private readonly pending = signal(false);
    private readonly currentFeedback = signal<Feedback | null>(null);

    readonly session = this.sessionState.asReadonly();
    readonly kind = this.selectedKind.asReadonly();
    readonly includeArchived = this.showArchived.asReadonly();
    readonly templates = this.currentTemplates.asReadonly();
    readonly editor = this.currentEditor.asReadonly();
    readonly busy = this.pending.asReadonly();
    readonly feedback = this.currentFeedback.asReadonly();

    async checkSession(): Promise<void> {
        await this.run('session', async () => {
            this.sessionState.set({ state: 'checking' });
            await this.openSession();
        });
    }

    async login(username: string, password: string): Promise<void> {
        await this.run('login', async () => {
            await firstValueFrom(this.api.login(username, password));
            await this.openSession();
        });
    }

    async logout(): Promise<void> {
        await this.run('change', async () => {
            await firstValueFrom(this.api.logout());
            this.clearSession();
        });
    }

    async selectKind(kind: TemplateKind): Promise<void> {
        if (kind === this.kind()) {
            return;
        }
        await this.run('load', async () => {
            this.selectedKind.set(kind);
            this.currentEditor.set(null);
            this.currentTemplates.set([]);
            await this.fetchTemplates();
        });
    }

    async setIncludeArchived(includeArchived: boolean): Promise<void> {
        await this.run('load', async () => {
            this.showArchived.set(includeArchived);
            this.currentTemplates.set([]);
            await this.fetchTemplates();
        });
    }

    async reload(): Promise<void> {
        await this.run('load', async () => {
            await this.fetchTemplates();
        });
    }

    newTemplate(): void {
        if (!this.busy()) {
            this.currentFeedback.set(null);
            this.currentEditor.set({ kind: this.kind() });
        }
    }

    async editTemplate(templateId: string): Promise<void> {
        await this.run('load', async () => {
            const kind = this.kind();
            const saved = await firstValueFrom(this.api.get(kind, templateId));
            this.currentEditor.set({ kind, saved });
        });
    }

    closeEditor(): void {
        if (!this.busy()) {
            this.currentEditor.set(null);
            this.currentFeedback.set(null);
        }
    }

    async save(template: TemplateWrite): Promise<void> {
        const editor = this.editor();
        if (!editor) {
            return;
        }
        await this.run('save', async () => {
            const request = editor.saved ? this.api.update(editor.kind, editor.saved, template) : this.api.create(editor.kind, template);
            await firstValueFrom(request);
            await this.finishEdit('saved');
        });
    }

    async setArchived(isArchived: boolean): Promise<void> {
        const editor = this.editor();
        const saved = editor?.saved;
        if (!saved) {
            return;
        }
        await this.run('change', async () => {
            await firstValueFrom(this.api.archive(editor.kind, saved, isArchived));
            await this.finishEdit(isArchived ? 'archived' : 'restored');
        });
    }

    async deleteTemplate(): Promise<void> {
        const editor = this.editor();
        const saved = editor?.saved;
        if (!saved) {
            return;
        }
        await this.run('change', async () => {
            await firstValueFrom(this.api.delete(editor.kind, saved));
            await this.finishEdit('deleted');
        });
    }

    async importFile(file: File): Promise<boolean> {
        let imported = false;
        await this.run('import', async () => {
            if (file.size > maximumImportBytes) {
                this.currentFeedback.set({ code: 'fileTooLarge' });
                return;
            }
            const result = await firstValueFrom(this.api.importDocument(await file.text()));
            imported = true;
            this.currentFeedback.set({ code: 'imported', characters: result.characterIds.length, monsters: result.monsterIds.length });
            await this.refreshTemplates();
        });
        return imported;
    }

    private async openSession(): Promise<void> {
        const session = await firstValueFrom(this.api.session());
        this.sessionState.set({ state: 'signedIn', session });
        await this.refreshTemplates();
    }

    private clearSession(): void {
        this.currentEditor.set(null);
        this.currentTemplates.set([]);
        this.showArchived.set(false);
        this.selectedKind.set('characters');
        this.sessionState.set({ state: 'signedOut' });
    }

    private async fetchTemplates(): Promise<void> {
        this.currentTemplates.set(await firstValueFrom(this.api.list(this.kind(), this.includeArchived())));
    }

    private async finishEdit(code: SuccessCode): Promise<void> {
        this.currentEditor.set(null);
        this.currentFeedback.set({ code });
        await this.refreshTemplates();
    }

    private async refreshTemplates(): Promise<void> {
        try {
            await this.fetchTemplates();
        } catch (error) {
            this.reportFailure(error, 'load');
        }
    }

    private async run(action: Action, work: () => Promise<void>): Promise<void> {
        if (this.busy()) {
            return;
        }
        this.pending.set(true);
        this.currentFeedback.set(null);
        try {
            await work();
        } catch (error) {
            this.reportFailure(error, action);
        } finally {
            this.pending.set(false);
        }
    }

    private reportFailure(error: unknown, action: Action): void {
        let code = failureCodes[action];
        let issues: FieldIssue[] | undefined;
        if (error instanceof HttpErrorResponse) {
            if (error.status === 401) {
                this.clearSession();
                if (action === 'session') {
                    return;
                }
                code = action === 'login' ? 'invalidLogin' : 'sessionExpired';
            } else if (error.status === 429) {
                code = 'rateLimited';
            } else if (error.status === 412 || error.status === 428) {
                code = 'stale';
            } else if (error.status === 409 && error.error?.code === 'templateReferenced') {
                code = 'referenced';
            } else if (error.status === 409 && error.error?.code === 'importAlreadyProcessed') {
                code = 'duplicateImport';
            } else if (action === 'import' && error.status === 413) {
                code = 'fileTooLarge';
            } else if (action === 'import' && (error.status === 400 || error.status === 415)) {
                code = 'invalidImport';
                issues = fieldIssues(error.error?.errors);
            } else if (action === 'save' && error.status === 400) {
                code = 'invalidTemplate';
                issues = fieldIssues(error.error?.errors);
            }
        }
        if (action === 'session') {
            this.sessionState.set({ state: 'unavailable' });
        }
        this.currentFeedback.set({ code, issues });
    }
}

function fieldIssues(errors: unknown): FieldIssue[] {
    if (!errors || typeof errors !== 'object') {
        return [];
    }
    return Object.entries(errors).flatMap(([path, codes]) => {
        if (!Array.isArray(codes)) {
            return [];
        }
        return codes.filter((code): code is string => typeof code === 'string').map((code) => ({ path, code }));
    });
}
