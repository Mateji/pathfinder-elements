import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable, switchMap } from 'rxjs';
import type { ImportResult, LibrarySession, LibraryTemplate, TemplateKind, TemplateWrite, VersionedTemplate } from './library-model';

@Injectable({ providedIn: 'root' })
export class LibraryApi {
    private readonly http = inject(HttpClient);

    session(): Observable<LibrarySession> {
        return this.http.get<LibrarySession>('/api/auth/session');
    }

    login(username: string, password: string): Observable<void> {
        return this.csrfHeaders().pipe(switchMap((headers) => {
            return this.http.post<void>('/api/auth/login', { username, password }, { headers });
        }));
    }

    logout(): Observable<void> {
        return this.csrfHeaders().pipe(switchMap((headers) => {
            return this.http.post<void>('/api/auth/logout', null, { headers });
        }));
    }

    list(kind: TemplateKind, includeArchived: boolean): Observable<LibraryTemplate[]> {
        return this.http.get<LibraryTemplate[]>(`/api/${kind}`, { params: { includeArchived } });
    }

    get(kind: TemplateKind, templateId: string): Observable<VersionedTemplate> {
        return this.http.get<LibraryTemplate>(this.detailPath(kind, templateId), { observe: 'response' }).pipe(map((response) => {
            const etag = response.headers.get('ETag');
            if (!response.body || !etag) {
                throw new Error('Incomplete template response');
            }
            return { template: response.body, etag };
        }));
    }

    create(kind: TemplateKind, template: TemplateWrite): Observable<LibraryTemplate> {
        return this.csrfHeaders().pipe(switchMap((headers) => {
            return this.http.post<LibraryTemplate>(`/api/${kind}`, template, { headers });
        }));
    }

    update(kind: TemplateKind, saved: VersionedTemplate, template: TemplateWrite): Observable<LibraryTemplate> {
        return this.csrfHeaders(saved.etag).pipe(switchMap((headers) => {
            return this.http.put<LibraryTemplate>(this.detailPath(kind, saved.template.id), template, { headers });
        }));
    }

    archive(kind: TemplateKind, saved: VersionedTemplate, isArchived: boolean): Observable<LibraryTemplate> {
        return this.csrfHeaders(saved.etag).pipe(switchMap((headers) => {
            return this.http.put<LibraryTemplate>(`${this.detailPath(kind, saved.template.id)}/archive`, { isArchived }, { headers });
        }));
    }

    delete(kind: TemplateKind, saved: VersionedTemplate): Observable<void> {
        return this.csrfHeaders(saved.etag).pipe(switchMap((headers) => {
            return this.http.delete<void>(this.detailPath(kind, saved.template.id), { headers });
        }));
    }

    importDocument(document: string): Observable<ImportResult> {
        return this.csrfHeaders().pipe(switchMap((headers) => {
            return this.http.post<ImportResult>('/api/library/import', document, {
                headers: headers.set('Content-Type', 'application/json')
            });
        }));
    }

    private csrfHeaders(etag?: string): Observable<HttpHeaders> {
        return this.http.get<{ token: string }>('/api/auth/csrf').pipe(map(({ token }) => {
            const headers = new HttpHeaders({ 'X-CSRF-TOKEN': token });
            return etag ? headers.set('If-Match', etag) : headers;
        }));
    }

    private detailPath(kind: TemplateKind, templateId: string): string {
        return `/api/${kind}/${encodeURIComponent(templateId)}`;
    }
}
