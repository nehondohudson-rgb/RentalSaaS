import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import * as XLSX from 'xlsx';

interface RentalUnit {
  id: number;
  unitNumber: string;
  monthlyRent: number;
  status: string;
  propertyId: number;
}

interface Property {
  id: number;
  name: string;
  address: string;
  ownerName: string;
  logoDataUrl: string;
  primaryColor: string;
  accentColor: string;
  createdAt: string;
  units: RentalUnit[];
}

interface Tenant {
  id: number;
  fullName: string;
  phoneNumber: string;
  email: string | null;
  nationalId: string | null;
  createdAt: string;
  isActive: boolean;
}

interface TenantForm {
  fullName: string;
  phoneNumber: string;
  email: string;
  nationalId: string;
}

@Pipe({ name: 'sumRent', standalone: true })
export class SumRentPipe implements PipeTransform {
  transform(units: RentalUnit[] | null | undefined): number {
    return (units ?? []).reduce((total, unit) => total + unit.monthlyRent, 0);
  }
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterOutlet, SumRentPipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'RentalSaaS';

  private readonly apiUrl = 'http://localhost:5081/api/Properties';
  private readonly tenantApi = 'http://localhost:5081/api/Tenants';

  properties: Property[] = [];
  propertyFormVisible = false;
  propertySaving = false;
  propertyError = '';
  propertyMessage = '';

  propertyForm = {
    name: '',
    address: '',
    ownerName: '',
    units: [
      { unitNumber: '', monthlyRent: 0, status: 'Vacant' }
    ]
  };

  loading = true;
  errorMessage = '';
  searchTerm = '';
  selectedView = 'Overview';
  sidebarCollapsed = false;

  selectedPropertyId: number | null = null;
  dashboardPropertyId: number | null = null;
  configForm = { name: '', address: '', ownerName: '' };
  logoPreviewUrl = '';
  configPrimaryColor = '#245b45';
  configAccentColor = '#eaf7ef';
  configNotice = '';
  configSaving = false;


  tenants: Tenant[] = [];
  tenantsLoading = false;
  tenantSearch = '';
  tenantError = '';
  tenantMessage = '';
  tenantFormVisible = false;
  tenantSaving = false;
  editingTenantId: number | null = null;
  tenantForm: TenantForm = this.emptyTenantForm();

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadProperties();
  }

  loadProperties(): void {
    this.loading = true;
    this.errorMessage = '';

    this.http.get<Property[]>(this.apiUrl).subscribe({
      next: data => {
        this.properties = data;

        if (this.dashboardPropertyId === null ||
            !data.some(property => property.id === this.dashboardPropertyId)) {
          this.dashboardPropertyId = data.length > 0 ? data[0].id : null;
        }
        if (this.selectedPropertyId === null && data.length > 0) {
          this.selectedPropertyId = data[0].id;
          this.loadConfigFromSelectedProperty();
        }
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load properties. Check that the RentalSaaS API is running and allows frontend access.';
        this.loading = false;
      }
    });
  }

  get filteredProperties(): Property[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.properties;

    return this.properties.filter(property =>
      property.name.toLowerCase().includes(term) ||
      property.address.toLowerCase().includes(term) ||
      property.ownerName.toLowerCase().includes(term)
    );
  }

  get dashboardProperty(): Property | undefined {
    return this.properties.find(
      property => property.id === this.dashboardPropertyId
    );
  }

  get dashboardTotalUnits(): number {
    return this.dashboardProperty?.units.length ?? 0;
  }

  get dashboardOccupiedUnits(): number {
    return (this.dashboardProperty?.units ?? []).filter(
      unit => unit.status.toLowerCase() === 'occupied'
    ).length;
  }

  get dashboardVacantUnits(): number {
    return (this.dashboardProperty?.units ?? []).filter(
      unit => unit.status.toLowerCase() === 'vacant'
    ).length;
  }

  get dashboardOccupancyRate(): number {
    return this.dashboardTotalUnits === 0
      ? 0
      : Math.round(
          this.dashboardOccupiedUnits / this.dashboardTotalUnits * 100
        );
  }

  get dashboardExpectedMonthlyRent(): number {
    return (this.dashboardProperty?.units ?? []).reduce(
      (total, unit) => total + Number(unit.monthlyRent || 0),
      0
    );
  }

  onDashboardPropertyChange(id: number | null): void {
    this.dashboardPropertyId = id;
  }
  get totalUnits(): number {
    return this.properties.reduce((total, property) => total + property.units.length, 0);
  }

  get occupiedUnits(): number {
    return this.properties.reduce(
      (total, property) =>
        total + property.units.filter(unit => unit.status.toLowerCase() === 'occupied').length,
      0
    );
  }

  get vacantUnits(): number {
    return this.properties.reduce(
      (total, property) =>
        total + property.units.filter(unit => unit.status.toLowerCase() === 'vacant').length,
      0
    );
  }

  get potentialMonthlyRent(): number {
    return this.properties.reduce(
      (total, property) =>
        total + property.units.reduce((sum, unit) => sum + unit.monthlyRent, 0),
      0
    );
  }

  openNewPropertyForm(): void {
    this.propertyForm = {
      name: '',
      address: '',
      ownerName: '',
      units: [
        { unitNumber: '', monthlyRent: 0, status: 'Vacant' }
      ]
    };
    this.propertyError = '';
    this.propertyMessage = '';
    this.propertyFormVisible = true;
  }

  cancelPropertyForm(): void {
    this.propertyFormVisible = false;
    this.propertyError = '';
  }

  addPropertyUnit(): void {
    this.propertyForm.units.push({
      unitNumber: '',
      monthlyRent: 0,
      status: 'Vacant'
    });
  }

  removePropertyUnit(index: number): void {
    if (this.propertyForm.units.length > 1) {
      this.propertyForm.units.splice(index, 1);
    }
  }

  saveProperty(): void {
    const name = this.propertyForm.name.trim();
    const address = this.propertyForm.address.trim();
    const ownerName = this.propertyForm.ownerName.trim();

    const units = this.propertyForm.units.map(unit => ({
      unitNumber: unit.unitNumber.trim(),
      monthlyRent: Number(unit.monthlyRent),
      status: unit.status
    }));

    if (!name || !address || !ownerName) {
      this.propertyError =
        'Property name, address and owner are required.';
      return;
    }

    if (units.some(unit =>
      !unit.unitNumber ||
      !Number.isFinite(unit.monthlyRent) ||
      unit.monthlyRent <= 0
    )) {
      this.propertyError =
        'Enter a unit number and monthly rent greater than zero for every unit.';
      return;
    }

    const unitNumbers = units.map(
      unit => unit.unitNumber.toLowerCase()
    );

    if (new Set(unitNumbers).size !== unitNumbers.length) {
      this.propertyError =
        'Each unit must have a unique unit number.';
      return;
    }

    this.propertySaving = true;
    this.propertyError = '';
    this.propertyMessage = '';

    const payload = { name, address, ownerName, units };

    this.http.post<Property>(this.apiUrl, payload).subscribe({
      next: () => {
        this.propertySaving = false;
        this.propertyFormVisible = false;
        this.propertyMessage = 'Property added successfully.';
        this.searchTerm = '';
        this.loadProperties();
      },
      error: error => {
        this.propertySaving = false;
        this.propertyError = error.status === 0
          ? 'Cannot connect to the API. Check that the backend is running on port 5081.'
          : `Could not save property (HTTP ${error.status}). Check the backend API request format.`;
      }
    });
  }
  get configProperty(): Property | undefined {
    return this.properties.find(
      property => property.id === this.selectedPropertyId
    );
  }

  onConfigPropertyChange(id: number | null): void {
    this.selectedPropertyId = id;
    this.logoPreviewUrl = '';
    this.configPrimaryColor = '#245b45';
    this.configAccentColor = '#eaf7ef';
    this.configNotice = '';
    this.loadConfigFromSelectedProperty();
  }

  loadConfigFromSelectedProperty(): void {
    const property = this.configProperty;

    if (!property) {
      this.configForm = { name: '', address: '', ownerName: '' };
      this.logoPreviewUrl = '';
      this.configPrimaryColor = '#245b45';
      this.configAccentColor = '#eaf7ef';
      return;
    }

    this.configForm = {
      name: property.name ?? '',
      address: property.address ?? '',
      ownerName: property.ownerName ?? ''
    };

    this.logoPreviewUrl = property.logoDataUrl ?? '';
    this.configPrimaryColor = property.primaryColor || '#245b45';
    this.configAccentColor = property.accentColor || '#eaf7ef';
  }

  onPropertyLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.configNotice = 'Choose a valid image file.';
      input.value = '';
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      this.configNotice = 'Choose an image smaller than 2 MB.';
      input.value = '';
      return;
    }

    const reader = new FileReader();

    reader.onload = () => {
      this.logoPreviewUrl = String(reader.result ?? '');
      this.configNotice = 'Logo preview ready. Select Save Changes to store it.';
    };

    reader.onerror = () => {
      this.configNotice = 'The selected image could not be read.';
    };

    reader.readAsDataURL(file);
    input.value = '';
  }

  savePropertyConfig(): void {
    const property = this.configProperty;

    if (!property) {
      this.configNotice = 'Select a property first.';
      return;
    }

    const name = this.configForm.name.trim();
    const address = this.configForm.address.trim();
    const ownerName = this.configForm.ownerName.trim();

    if (!name || !address || !ownerName) {
      this.configNotice = 'Property name, address and owner are required.';
      return;
    }

    const payload = {
      name,
      address,
      ownerName,
      logoDataUrl: this.logoPreviewUrl || '',
      primaryColor: this.configPrimaryColor,
      accentColor: this.configAccentColor
    };

    this.configSaving = true;
    this.configNotice = 'Saving property settings...';

    this.http.put<void>(`${this.apiUrl}/${property.id}`, payload).subscribe({
      next: () => {
        Object.assign(property, payload);
        this.configSaving = false;
        this.configNotice = 'Property settings saved successfully.';
      },
      error: error => {
        this.configSaving = false;
        this.configNotice = error.status === 0
          ? 'Cannot connect to the API. Check that the backend is running on port 5081.'
          : error.status === 400
            ? 'The API rejected the settings. Check the fields and ensure the logo is under 2 MB.'
            : `Could not save property settings (HTTP ${error.status}).`;
      }
    });
  }

  previewPropertyConfig(): void {
    if (!this.configProperty) {
      this.configNotice = 'Select a property first.';
      return;
    }

    if (!this.configForm.name.trim() ||
        !this.configForm.address.trim() ||
        !this.configForm.ownerName.trim()) {
      this.configNotice =
        'Property name, address and owner are required.';
      return;
    }

    this.configNotice =
      'Preview updated. These changes are not saved to the database yet.';
  }

  resetPropertyConfigPreview(): void {
    this.loadConfigFromSelectedProperty();
    this.configNotice = 'Preview reset to the saved property details.';
  }
  selectView(view: string): void {
    this.selectedView = view;

    if (view === 'Tenants') {
      this.loadTenants();
    }

    if (view === 'Property Config') {
      if (this.selectedPropertyId === null && this.properties.length > 0) {
        this.selectedPropertyId = this.properties[0].id;
      }
      this.loadConfigFromSelectedProperty();
    }
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  loadTenants(): void {
    this.tenantsLoading = true;
    this.tenantError = '';

    this.http.get<Tenant[]>(`${this.tenantApi}?activeOnly=false`).subscribe({
      next: data => {
        this.tenants = Array.isArray(data) ? data : [];
        this.tenantsLoading = false;
      },
      error: error => {
        this.tenantsLoading = false;
        this.tenantError = error.status === 0
          ? 'Cannot connect to the API. Check that the backend is running on port 5081.'
          : `Unable to load tenants (HTTP ${error.status}).`;
      }
    });
  }

  get filteredTenants(): Tenant[] {
    const term = this.tenantSearch.trim().toLowerCase();
    if (!term) return this.tenants;

    return this.tenants.filter(tenant =>
      tenant.fullName.toLowerCase().includes(term) ||
      tenant.phoneNumber.toLowerCase().includes(term) ||
      (tenant.email ?? '').toLowerCase().includes(term) ||
      (tenant.nationalId ?? '').toLowerCase().includes(term)
    );
  }

  openNewTenantForm(): void {
    this.editingTenantId = null;
    this.tenantForm = this.emptyTenantForm();
    this.tenantError = '';
    this.tenantMessage = '';
    this.tenantFormVisible = true;
  }

  editTenant(tenant: Tenant): void {
    this.editingTenantId = tenant.id;
    this.tenantForm = {
      fullName: tenant.fullName,
      phoneNumber: tenant.phoneNumber,
      email: tenant.email ?? '',
      nationalId: tenant.nationalId ?? ''
    };
    this.tenantError = '';
    this.tenantMessage = '';
    this.tenantFormVisible = true;
  }

  cancelTenantForm(): void {
    this.tenantFormVisible = false;
    this.editingTenantId = null;
    this.tenantForm = this.emptyTenantForm();
  }

  saveTenant(): void {
    const payload = {
      fullName: this.tenantForm.fullName.trim(),
      phoneNumber: this.tenantForm.phoneNumber.trim(),
      email: this.tenantForm.email.trim() || null,
      nationalId: this.tenantForm.nationalId.trim() || null
    };

    if (!payload.fullName || !payload.phoneNumber) {
      this.tenantError = 'Full name and phone number are required.';
      return;
    }

    this.tenantSaving = true;
    this.tenantError = '';
    this.tenantMessage = '';

    const request = this.editingTenantId === null
      ? this.http.post<Tenant>(this.tenantApi, payload)
      : this.http.put<Tenant>(`${this.tenantApi}/${this.editingTenantId}`, payload);

    request.subscribe({
      next: () => {
        this.tenantSaving = false;
        this.tenantFormVisible = false;
        this.tenantMessage = this.editingTenantId === null
          ? 'Tenant added successfully.'
          : 'Tenant updated successfully.';
        this.editingTenantId = null;
        this.tenantForm = this.emptyTenantForm();
        this.loadTenants();
      },
      error: error => {
        this.tenantSaving = false;
        this.tenantError = error.status === 409
          ? 'A tenant with that National ID already exists.'
          : error.status === 0
            ? 'Cannot connect to the API. Check that the backend is running.'
            : `Could not save tenant (HTTP ${error.status}). Check the required fields.`;
      }
    });
  }

  deactivateTenant(tenant: Tenant): void {
    if (!confirm(`Deactivate ${tenant.fullName}?`)) return;

    this.tenantError = '';
    this.tenantMessage = '';

    this.http.delete(`${this.tenantApi}/${tenant.id}`).subscribe({
      next: () => {
        this.tenantMessage = `${tenant.fullName} has been deactivated.`;
        this.loadTenants();
      },
      error: error => {
        this.tenantError = `Could not deactivate tenant (HTTP ${error.status}).`;
      }
    });
  }

  async onExcelSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.tenantError = '';
    this.tenantMessage = '';

    try {
      const workbook = XLSX.read(await file.arrayBuffer());
      const firstSheet = workbook.Sheets[workbook.SheetNames[0]];

      if (!firstSheet) {
        this.tenantError = 'The workbook has no readable worksheet.';
        return;
      }

      const rows = XLSX.utils.sheet_to_json<Record<string, unknown>>(firstSheet, {
        defval: ''
      });

      if (!rows.length) {
        this.tenantError = 'The worksheet contains no tenant rows.';
        return;
      }

      const normalise = (value: string) =>
        value.replace(/[\s_-]/g, '').toLowerCase();

      const getValue = (row: Record<string, unknown>, aliases: string[]): string => {
        const key = Object.keys(row).find(header =>
          aliases.includes(normalise(header))
        );
        return key ? String(row[key] ?? '').trim() : '';
      };

      let added = 0;
      let failed = 0;
      const problems: string[] = [];

      for (let index = 0; index < rows.length; index++) {
        const row = rows[index];
        const fullName = getValue(row, ['fullname', 'name', 'tenantname']);
        const phoneNumber = getValue(row, ['phonenumber', 'phone', 'mobile', 'telephone']);
        const email = getValue(row, ['email', 'emailaddress']);
        const nationalId = getValue(row, ['nationalid', 'idnumber', 'nationalidnumber']);

        if (!fullName || !phoneNumber) {
          failed++;
          problems.push(`Row ${index + 2}: full name or phone number is missing.`);
          continue;
        }

        try {
          await firstValueFrom(this.http.post<Tenant>(this.tenantApi, {
            fullName,
            phoneNumber,
            email: email || null,
            nationalId: nationalId || null
          }));
          added++;
        } catch (error: any) {
          failed++;
          const reason = error?.status === 409
            ? 'duplicate National ID'
            : `HTTP ${error?.status ?? 'error'}`;
          problems.push(`Row ${index + 2}: ${reason}.`);
        }
      }

      this.tenantMessage = `Import finished: ${added} added, ${failed} failed.`;
      this.tenantError = problems.slice(0, 8).join('\n');
      this.loadTenants();
    } catch {
      this.tenantError = 'Unable to read this Excel file. Use the provided .xlsx template.';
    } finally {
      input.value = '';
    }
  }

  downloadTenantTemplate(): void {
    const rows = [{
      FullName: 'Jane Wanjiku',
      PhoneNumber: '0712345678',
      Email: 'jane@example.com',
      NationalId: '12345678'
    }];

    const sheet = XLSX.utils.json_to_sheet(rows);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, sheet, 'Tenants');
    XLSX.writeFile(workbook, 'RentalSaaS_Tenant_Template.xlsx');
  }

  private emptyTenantForm(): TenantForm {
    return {
      fullName: '',
      phoneNumber: '',
      email: '',
      nationalId: ''
    };
  }
}
