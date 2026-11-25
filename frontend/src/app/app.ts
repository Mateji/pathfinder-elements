import { Component, DOCUMENT, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { I18nService } from './i18n/i18n.service';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, RouterLink, RouterLinkActive],
    templateUrl: './app.html',
    styleUrl: './app.css'
})
export class App {
    private readonly document = inject(DOCUMENT);
    private readonly title = inject(Title);
    private readonly router = inject(Router);
    private readonly currentUrl = toSignal(this.router.events.pipe(
        filter((event) => event instanceof NavigationEnd),
        map((event) => event.urlAfterRedirects)
    ), { initialValue: this.router.url });

    readonly i18n = inject(I18nService);

    constructor() {
        effect(() => {
            this.document.documentElement.lang = this.i18n.language();
            const messages = this.i18n.messages();
            const title = this.currentUrl().split(/[?#]/)[0] === '/library' ? messages.library.title : messages.initiativeTracker;
            this.title.setTitle(`${title} | Pathfinder Elements`);
        });
    }
}
