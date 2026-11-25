import { Component, DOCUMENT, effect, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterOutlet } from '@angular/router';
import { I18nService } from './i18n/i18n.service';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet],
    templateUrl: './app.html',
    styleUrl: './app.css'
})
export class App {
    private readonly document = inject(DOCUMENT);
    private readonly title = inject(Title);

    readonly i18n = inject(I18nService);

    constructor() {
        effect(() => {
            this.document.documentElement.lang = this.i18n.language();
            this.title.setTitle(`${this.i18n.messages().initiativeTracker} | Pathfinder Elements`);
        });
    }
}
