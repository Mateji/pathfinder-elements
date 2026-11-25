import { computed, DOCUMENT, inject, Injectable, signal } from '@angular/core';
import { type Language, messages } from './messages';

const languageStorageKey = 'pathfinder-elements.language';

function isLanguage(value: string | null | undefined): value is Language {
    return value === 'de' || value === 'en';
}

@Injectable({ providedIn: 'root' })
export class I18nService {
    private readonly browserWindow = inject(DOCUMENT).defaultView;
    private readonly selectedLanguage = signal(this.initialLanguage());

    readonly language = this.selectedLanguage.asReadonly();
    readonly messages = computed(() => {
        return messages[this.language()];
    });

    setLanguage(language: string): void {
        if (!isLanguage(language)) {
            return;
        }

        this.selectedLanguage.set(language);
        try {
            this.browserWindow?.localStorage.setItem(languageStorageKey, language);
        } catch {
            // The in-memory selection still works when storage is unavailable.
        }
    }

    private initialLanguage(): Language {
        try {
            const savedLanguage = this.browserWindow?.localStorage.getItem(languageStorageKey);
            if (isLanguage(savedLanguage)) {
                return savedLanguage;
            }
        } catch {
            // Fall back to the browser preferences when storage is unavailable.
        }

        for (const locale of this.browserWindow?.navigator.languages ?? []) {
            const language = locale.toLowerCase().split('-')[0];
            if (isLanguage(language)) {
                return language;
            }
        }

        return 'en';
    }
}
