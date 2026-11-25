import { HttpClient, type HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable, switchMap } from 'rxjs';
import { loadCsrfHeaders } from '../api/csrf-headers';
import type { TemplateKind } from '../library/library-model';

export interface EncounterSummary {
    id: string;
    name: string;
    participantCount: number;
}

export interface EncounterParticipant {
    id: string;
    number: number;
    color: string;
    kind: TemplateKind;
    templateId: string;
    name: string;
    maxHitPoints: number;
    isArchived: boolean;
}

export interface PreparedEncounter {
    id: string;
    name: string;
    participants: EncounterParticipant[];
}

export interface VersionedEncounter {
    encounter: PreparedEncounter;
    etag: string;
}

export interface ParticipantSelection {
    kind: TemplateKind;
    templateId: string;
    quantity?: number;
}

const encountersPath = '/api/encounters';

function versioned(response: HttpResponse<PreparedEncounter>): VersionedEncounter {
    const etag = response.headers.get('ETag');
    if (!etag || !response.body) {
        throw new Error('Incomplete encounter response');
    }
    return { etag, encounter: response.body };
}

@Injectable({ providedIn: 'root' })
export class EncounterApi {
    private readonly http = inject(HttpClient);

    list(): Observable<EncounterSummary[]> {
        return this.http.get<EncounterSummary[]>(encountersPath);
    }

    get(id: string): Observable<VersionedEncounter> {
        return this.http.get<PreparedEncounter>(this.encounterPath(id), { observe: 'response' }).pipe(map(versioned));
    }

    create(name: string): Observable<VersionedEncounter> {
        return this.write('POST', encountersPath, { name });
    }

    rename(saved: VersionedEncounter, name: string): Observable<VersionedEncounter> {
        return this.write('PUT', this.encounterPath(saved.encounter.id), { name }, saved.etag);
    }

    add(saved: VersionedEncounter, selection: ParticipantSelection): Observable<VersionedEncounter> {
        return this.write('POST', `${this.encounterPath(saved.encounter.id)}/participants`, selection, saved.etag);
    }

    changeColor(saved: VersionedEncounter, participantId: string, color: string): Observable<VersionedEncounter> {
        return this.write('PUT', `${this.participantPath(saved, participantId)}/color`, { color }, saved.etag);
    }

    remove(saved: VersionedEncounter, participantId: string): Observable<VersionedEncounter> {
        return this.write('DELETE', this.participantPath(saved, participantId), null, saved.etag);
    }

    private write(method: string, path: string, body: object | null, etag?: string): Observable<VersionedEncounter> {
        return loadCsrfHeaders(this.http, etag).pipe(switchMap((headers) => {
            return this.http.request<PreparedEncounter>(method, path, { body, headers, observe: 'response' });
        }), map(versioned));
    }

    private encounterPath(id: string): string {
        return `${encountersPath}/${encodeURIComponent(id)}`;
    }

    private participantPath(saved: VersionedEncounter, participantId: string): string {
        return `${this.encounterPath(saved.encounter.id)}/participants/${encodeURIComponent(participantId)}`;
    }
}
