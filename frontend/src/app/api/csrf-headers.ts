import { type HttpClient, HttpHeaders } from '@angular/common/http';
import { map, type Observable } from 'rxjs';

export function loadCsrfHeaders(http: HttpClient, etag?: string): Observable<HttpHeaders> {
    return http.get<{ token: string }>('/api/auth/csrf').pipe(map(({ token }) => {
        const headers = new HttpHeaders({ 'X-CSRF-TOKEN': token });
        return etag ? headers.set('If-Match', etag) : headers;
    }));
}
