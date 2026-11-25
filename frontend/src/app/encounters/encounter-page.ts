import { ChangeDetectionStrategy, Component, computed, DOCUMENT, inject, type OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { I18nService } from '../i18n/i18n.service';
import type { TemplateKind } from '../library/library-model';
import { EncounterStore } from './encounter-store';

@Component({
    selector: 'app-encounter-page',
    imports: [FormsModule, RouterLink],
    providers: [EncounterStore],
    templateUrl: './encounter-page.html',
    styleUrl: './encounter-page.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class EncounterPage implements OnInit {
    readonly i18n = inject(I18nService);
    readonly store = inject(EncounterStore);
    readonly secureOrigin = inject(DOCUMENT).location?.protocol === 'https:';
    readonly createName = signal('');
    readonly renameName = signal('');
    readonly kind = signal<TemplateKind>('characters');
    readonly templateId = signal('');
    readonly quantity = signal<number | null>(1);
    readonly maximumQuantity = 100;
    readonly templates = computed(() => this.store.choices()[this.kind()]);
    readonly hasSelection = computed(() => this.templates().some((template) => template.id === this.templateId()));
    readonly validQuantity = computed(() => {
        const quantity = this.quantity();
        return this.kind() === 'characters' || (quantity !== null && Number.isInteger(quantity) && quantity >= 1 && quantity <= this.maximumQuantity);
    });
    readonly participants = computed(() => {
        const language = this.i18n.language();
        return [...(this.store.encounter()?.participants ?? [])].sort((left, right) => {
            return left.kind.localeCompare(right.kind) || left.name.localeCompare(right.name, language);
        });
    });

    ngOnInit(): void {
        if (this.secureOrigin) {
            void this.reload();
        }
    }

    async reload(): Promise<void> {
        if (await this.store.reload()) {
            this.renameName.set(this.store.encounter()?.name ?? '');
            if (!this.hasSelection()) {
                this.selectTemplate('');
            }
        }
    }

    async open(id: string): Promise<void> {
        if (await this.store.open(id)) {
            this.renameName.set(this.store.encounter()?.name ?? '');
            this.selectTemplate('');
        }
    }

    async create(): Promise<void> {
        if (this.createName().trim() && await this.store.create(this.createName())) {
            this.createName.set('');
            this.renameName.set(this.store.encounter()?.name ?? '');
            this.selectTemplate('');
        }
    }

    async rename(): Promise<void> {
        if (this.renameName().trim() && await this.store.rename(this.renameName())) {
            this.renameName.set(this.store.encounter()?.name ?? '');
        }
    }

    selectKind(kind: TemplateKind): void {
        this.kind.set(kind);
        this.selectTemplate('');
    }

    selectTemplate(templateId: string): void {
        this.templateId.set(templateId);
        this.quantity.set(1);
    }

    async add(): Promise<void> {
        if (!this.hasSelection() || !this.validQuantity()) {
            return;
        }
        const kind = this.kind();
        const quantity = kind === 'monsters' ? this.quantity() ?? undefined : undefined;
        if (await this.store.add({ kind, templateId: this.templateId(), quantity })) {
            this.selectTemplate('');
        }
    }
}
