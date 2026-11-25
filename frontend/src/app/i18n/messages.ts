import { encounterMessages } from './encounter-messages';
import { libraryMessages } from './library-messages';

export type Language = 'de' | 'en';

interface Messages {
    readonly language: string;
    readonly initiativeTracker: string;
    readonly noCombatants: string;
    readonly previousTurn: string;
    readonly nextTurn: string;
    readonly armorClass: (value: number) => string;
    readonly navigation: string;
    readonly library: typeof libraryMessages.en;
    readonly encounters: typeof encounterMessages.en;
}

export const messages: Readonly<Record<Language, Messages>> = {
    de: {
        language: 'Sprache',
        initiativeTracker: 'Initiative-Tracker',
        noCombatants: 'Keine Kampfteilnehmer verfügbar.',
        previousTurn: 'Vorheriger Zug',
        nextTurn: 'Nächster Zug',
        navigation: 'Hauptnavigation',
        library: libraryMessages.de,
        encounters: encounterMessages.de,
        armorClass: (value) => {
            return `Rüstungsklasse ${value}`;
        }
    },
    en: {
        language: 'Language',
        initiativeTracker: 'Initiative Tracker',
        noCombatants: 'No combatants available.',
        previousTurn: 'Previous turn',
        nextTurn: 'Next turn',
        navigation: 'Main navigation',
        library: libraryMessages.en,
        encounters: encounterMessages.en,
        armorClass: (value) => {
            return `Armor Class ${value}`;
        }
    }
};
