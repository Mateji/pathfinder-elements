import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom, forkJoin, type Observable } from 'rxjs';
import { LibraryApi } from '../library/library-api';
import type { LibraryTemplate } from '../library/library-model';
import { EncounterApi, type EncounterSummary, type ParticipantSelection, type VersionedEncounter } from './encounter-api';

type SessionState = 'checking' | 'ready' | 'signedOut' | 'unavailable';
type Feedback = 'saved' | 'loadFailed' | 'saveFailed' | 'unavailableTemplate' | 'stale';

@Injectable()
export class EncounterStore {
    private readonly api = inject(EncounterApi);
    private readonly library = inject(LibraryApi);
    private readonly status = signal<SessionState>('checking');
    private readonly pending = signal(false);
    private readonly currentFeedback = signal<Feedback | null>(null);
    private readonly summaries = signal<EncounterSummary[]>([]);
    private readonly active = signal<VersionedEncounter | null>(null);
    private readonly characters = signal<LibraryTemplate[]>([]);
    private readonly monsters = signal<LibraryTemplate[]>([]);

    readonly encounter = computed(() => this.active()?.encounter ?? null);
    readonly savedEncounters = computed(() => [...this.summaries()].sort((left, right) => left.name.localeCompare(right.name)));
    readonly choices = computed(() => ({ characters: this.characters(), monsters: this.monsters() }));
    readonly session = this.status.asReadonly();
    readonly busy = this.pending.asReadonly();
    readonly feedback = this.currentFeedback.asReadonly();

    async reload(): Promise<boolean> {
        return this.run('loadFailed', async () => {
            if (this.status() !== 'ready') {
                this.status.set('checking');
                await firstValueFrom(this.library.session());
                this.status.set('ready');
            }
            const currentId = this.encounter()?.id;
            const { summaries, characters, monsters } = await firstValueFrom(forkJoin({
                summaries: this.api.list(),
                characters: this.library.list('characters', false),
                monsters: this.library.list('monsters', false)
            }));
            const active = currentId && summaries.some((summary) => summary.id === currentId)
                ? await firstValueFrom(this.api.get(currentId)) : null;
            this.summaries.set(summaries);
            this.characters.set(characters);
            this.monsters.set(monsters);
            this.active.set(active);
        });
    }

    async open(id: string): Promise<boolean> {
        return this.run('loadFailed', async () => {
            this.active.set(await firstValueFrom(this.api.get(id)));
        });
    }

    async create(name: string): Promise<boolean> {
        return this.save(this.api.create(name));
    }

    async rename(name: string): Promise<boolean> {
        const saved = this.active();
        return saved ? this.save(this.api.rename(saved, name)) : false;
    }

    async add(selection: ParticipantSelection): Promise<boolean> {
        const saved = this.active();
        return saved ? this.save(this.api.add(saved, selection)) : false;
    }

    async changeColor(participantId: string, color: string): Promise<boolean> {
        const saved = this.active();
        return saved ? this.save(this.api.changeColor(saved, participantId, color)) : false;
    }

    async remove(participantId: string): Promise<boolean> {
        const saved = this.active();
        return saved ? this.save(this.api.remove(saved, participantId)) : false;
    }

    private async save(request: Observable<VersionedEncounter>): Promise<boolean> {
        return this.run('saveFailed', async () => {
            const saved = await firstValueFrom(request);
            this.active.set(saved);
            const { id, name, participants } = saved.encounter;
            this.summaries.update((summaries) => [...summaries.filter((summary) => summary.id !== id),
                { id, name, participantCount: participants.length }]);
            this.currentFeedback.set('saved');
        });
    }

    private async run(failure: Feedback, work: () => Promise<void>): Promise<boolean> {
        if (this.busy()) {
            return false;
        }
        this.pending.set(true);
        this.currentFeedback.set(null);
        try {
            await work();
            return true;
        } catch (error) {
            this.reportFailure(error, failure);
            return false;
        } finally {
            this.pending.set(false);
        }
    }

    private reportFailure(error: unknown, fallback: Feedback): void {
        if (error instanceof HttpErrorResponse && error.status === 401) {
            this.active.set(null);
            this.summaries.set([]);
            this.characters.set([]);
            this.monsters.set([]);
            this.status.set('signedOut');
            return;
        }
        if (this.status() === 'checking') {
            this.status.set('unavailable');
        }
        if (error instanceof HttpErrorResponse && error.status === 412) {
            this.currentFeedback.set('stale');
        } else if (error instanceof HttpErrorResponse && error.status === 404 && error.error?.code === 'templateUnavailable') {
            this.currentFeedback.set('unavailableTemplate');
        } else {
            this.currentFeedback.set(fallback);
        }
    }
}
