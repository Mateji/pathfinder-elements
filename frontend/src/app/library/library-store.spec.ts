import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { LibraryApi } from './library-api';
import type { VersionedTemplate } from './library-model';
import { LibraryStore } from './library-store';

const saved: VersionedTemplate = {
    etag: '"first-version"',
    template: {
        id: 'pc-id', name: 'Original', isArchived: false,
        statistics: {
            maxHitPoints: 8, initiativeModifier: 0, armorClass: 12, touchArmorClass: 11, flatFootedArmorClass: 11,
            fortitudeSave: 2, reflexSave: 1, willSave: 0
        }
    }
};

describe('LibraryStore', () => {
    it('keeps the original selection and reports an unconfirmed save when the ETag is stale', async () => {
        const api = {
            session: () => of({ userId: 'owner-id', username: 'gm' }),
            list: vi.fn(() => of([saved.template])),
            get: () => of(saved),
            update: vi.fn(() => throwError(() => new HttpErrorResponse({ status: 412 })))
        };
        TestBed.configureTestingModule({ providers: [LibraryStore, { provide: LibraryApi, useValue: api }] });
        const store = TestBed.inject(LibraryStore);
        await store.checkSession();
        await store.editTemplate(saved.template.id);
        const draft = { name: 'Draft', statistics: saved.template.statistics };
        await store.save(draft);

        expect(api.update).toHaveBeenCalledWith('characters', saved, draft);
        expect(store.editor()?.saved).toBe(saved);
        expect(store.templates()).toEqual([saved.template]);
        expect(store.feedback()?.code).toBe('stale');
        expect(api.list).toHaveBeenCalledTimes(1);
    });

    it('clears previously loaded private data when the session is rejected', async () => {
        const api = {
            session: () => of({ userId: 'owner-id', username: 'gm' }),
            list: vi.fn(() => of([saved.template])),
            get: () => throwError(() => new HttpErrorResponse({ status: 401 }))
        };
        TestBed.configureTestingModule({ providers: [LibraryStore, { provide: LibraryApi, useValue: api }] });
        const store = TestBed.inject(LibraryStore);
        await store.checkSession();
        await store.editTemplate(saved.template.id);
        expect(store.session().state).toBe('signedOut');
        expect(store.templates()).toEqual([]);
        expect(store.editor()).toBeNull();
        expect(store.feedback()?.code).toBe('sessionExpired');
    });
});
