import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Combatant } from '../models/combatant.model';

@Injectable({
    providedIn: 'root'
})
export class EncounterService {
    private _http = inject(HttpClient);

    getEncounter(name: string): Observable<Combatant[]> {
        return this._http.get<Combatant[]>(`assets/encounters/${name}.json`);
    }
}
