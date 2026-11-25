import { HttpHeaders, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { LibraryApi } from './library-api';
import type { LibraryTemplate } from './library-model';

const monster: LibraryTemplate = {
    id: 'monster-1',
    name: 'Testmonster',
    isArchived: false,
    statistics: {
        maxHitPoints: 9, initiativeModifier: -1, armorClass: 14, touchArmorClass: 12, flatFootedArmorClass: 12,
        fortitudeSave: 3, reflexSave: 0, willSave: -1
    },
    attackSequences: []
};

describe('LibraryApi', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    });

    afterEach(() => {
        TestBed.inject(HttpTestingController).verify();
    });

    it('uses the current CSRF token and the exact ETag for a template change', async () => {
        const api = TestBed.inject(LibraryApi);
        const http = TestBed.inject(HttpTestingController);
        const detail = firstValueFrom(api.get('monsters', monster.id));
        http.expectOne('/api/monsters/monster-1').flush(monster, { headers: new HttpHeaders().set('ETag', '"version-1"') });
        const saved = await detail;
        const archived = firstValueFrom(api.archive('monsters', saved, true));
        http.expectOne('/api/auth/csrf').flush({ token: 'csrf-session' });
        const request = http.expectOne({ method: 'PUT', url: '/api/monsters/monster-1/archive' });
        expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('csrf-session');
        expect(request.request.headers.get('If-Match')).toBe('"version-1"');
        expect(request.request.body).toEqual({ isArchived: true });
        request.flush({ ...monster, isArchived: true });
        expect((await archived).isArchived).toBe(true);
    });

    it('sends the file text as JSON for the original server validation', async () => {
        const api = TestBed.inject(LibraryApi);
        const http = TestBed.inject(HttpTestingController);
        const document = '{ "schemaVersion": 1, "schemaVersion": 2 }';
        const result = firstValueFrom(api.importDocument(document));
        http.expectOne('/api/auth/csrf').flush({ token: 'csrf-session' });
        const request = http.expectOne({ method: 'POST', url: '/api/library/import' });
        expect(request.request.headers.get('Content-Type')).toBe('application/json');
        expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('csrf-session');
        expect(request.request.body).toBe(document);
        request.flush({ schemaVersion: 1, characterIds: ['character-id'], monsterIds: [] });
        expect((await result).characterIds).toEqual(['character-id']);
    });
});
