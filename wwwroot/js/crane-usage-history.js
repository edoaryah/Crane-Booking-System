/**
 * CraneUsageHistory module - Complete Implementation
 * Updated to match MaintenanceHistory layout (Portrait orientation)
 * Manages the crane usage history page with AJAX loading, filtering, pagination and export
 */
var CraneUsageHistory = (function () {
  // Private variables
  var config = {
    tableContainerId: 'tableContainer',
    formId: 'filterForm',
    filterInputSelector: 'select, input[type="date"]',
    searchInputId: 'GlobalSearch',
    exportContainerId: 'exportButtons',
    pageNumberId: 'PageNumber',
    pageSizeId: 'PageSize',
    resetBtnId: 'resetFilterBtn',
    getTableUrl: '',
    exportTitle: 'Riwayat Penggunaan Crane',
    companyName: 'PT. KALTIM PRIMA COAL',
    currentUser: 'System',
    companyLogo: '',
    logoType: 'svg'
  };

  var dataTable = null;
  var searchTimeout = null;

  // ===== LOGO HANDLING FUNCTIONS =====
  /**
   * Set company logo dengan format yang benar
   */
  function setCompanyLogo(logoData) {
    if (!logoData) {
      config.companyLogo = '';
      config.logoType = null;
      return;
    }

    // Jika sudah berupa data URI
    if (logoData.startsWith('data:')) {
      config.companyLogo = logoData;
      config.logoType = 'image';
    }
    // Jika berupa base64 SVG (seperti yang user punya)
    else if (logoData.startsWith('PHN2Zy')) {
      try {
        config.companyLogo = atob(logoData); // Decode ke SVG string
        config.logoType = 'svg';
      } catch (error) {
        console.error('Error decoding SVG base64:', error);
        config.companyLogo = 'data:image/svg+xml;base64,' + logoData;
        config.logoType = 'image';
      }
    }
    // Jika berupa SVG string langsung
    else if (logoData.includes('<svg')) {
      config.companyLogo = logoData;
      config.logoType = 'svg';
    }
    // Default: treat as base64 image
    else {
      config.companyLogo = 'data:image/png;base64,' + logoData;
      config.logoType = 'image';
    }

    console.log('Logo set successfully. Type:', config.logoType);
  }

  // ===== PDF FUNCTIONS =====
  /**
   * Get current filter information for PDF header
   */
  function getFilterInfo() {
    var crane = $('#CraneId option:selected').text();
    var startDate = $('#StartDate').val();
    var endDate = $('#EndDate').val();
    var status = $('#IsFinalized option:selected').text();
    var search = $('#GlobalSearch').val();
    var filterParts = [];

    if (crane && crane !== '-- Semua Crane --') {
      filterParts.push('Crane: ' + crane);
    }

    if (startDate && endDate) {
      filterParts.push('Periode: ' + startDate + ' s/d ' + endDate);
    } else if (startDate) {
      filterParts.push('Dari: ' + startDate);
    } else if (endDate) {
      filterParts.push('Sampai: ' + endDate);
    }

    if (status && status !== '-- Semua Status --') {
      filterParts.push('Status: ' + status);
    }

    if (search) {
      filterParts.push('Pencarian: "' + search + '"');
    }

    return filterParts.length > 0 ? filterParts.join(' | ') : 'Semua Data';
  }

  /**
   * Create proper document definition sesuai dokumentasi pdfMake (Portrait Layout)
   */
  function createDocumentDefinition(tableData) {
    var currentFilterInfo = getFilterInfo();
    var currentDate = new Date().toLocaleDateString('id-ID', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });

    // Document definition object sesuai dokumentasi pdfMake
    var docDefinition = {
      // Page configuration - PORTRAIT seperti maintenance history
      pageSize: 'A4',
      pageOrientation: 'portrait',
      pageMargins: [40, config.companyLogo ? 100 : 80, 40, 60], // Dynamic top margin based on logo

      // Document metadata
      info: {
        title: config.exportTitle,
        author: config.companyName,
        subject: 'Laporan Riwayat Penggunaan Crane',
        creator: 'Crane Booking System'
      },

      // Header function (dynamic) - Logo di kiri, Title di kanan
      header: function (currentPage, pageCount, pageSize) {
        var headerContent = {
          columns: [],
          margin: [40, 20, 40, 20]
        };

        // Logo column (jika ada)
        if (config.companyLogo) {
          if (config.logoType === 'svg') {
            headerContent.columns.push({
              svg: config.companyLogo,
              width: 80,
              height: 45
            });
          } else {
            headerContent.columns.push({
              image: config.companyLogo,
              width: 80,
              height: 45
            });
          }

          // Spacer column - PENTING untuk push content ke kanan
          headerContent.columns.push({
            width: '*',
            text: '' // Empty spacer
          });
        }

        // Page info column - sekarang akan benar-benar di kanan
        headerContent.columns.push({
          width: 'auto',
          stack: [
            {
              text: 'Hal. ' + currentPage + ' dari ' + pageCount,
              style: 'pageInfo'
            },
            {
              text: config.exportTitle,
              style: 'documentTitle'
            },
            {
              text: 'Crane Booking System',
              style: 'companySubtitle',
              margin: [0, 5, 0, 0]
            }
          ],
          alignment: 'right'
        });

        return headerContent;
      },

      // Footer function (dynamic)
      footer: function (currentPage, pageCount) {
        return {
          columns: [
            {
              text: 'Dicetak pada: ' + currentDate,
              style: 'footerLeft'
            },
            {
              text: 'Dicetak oleh: ' + config.currentUser,
              style: 'footerRight',
              alignment: 'right'
            }
          ],
          margin: [40, 10, 40, 0]
        };
      },

      // Main content
      content: [
        // Filter information
        {
          text: 'Filter: ' + currentFilterInfo,
          style: 'filterInfo',
          margin: [0, 0, 0, 15]
        },

        // Main table dengan layout zebra tanpa border bold
        // OPTIMIZED UNTUK PORTRAIT - Mengurangi kolom dan menyesuaikan lebar
        {
          style: 'tableExample',
          table: {
            headerRows: 1,
            // Table widths untuk PORTRAIT - disesuaikan dengan jumlah kolom yang lebih sedikit
            // Menggabungkan beberapa kolom untuk menghemat space
            widths: [25, 'auto', 'auto', 50, 50, 50, 50, 50, '*'],
            body: tableData
          },
          layout: {
            fillColor: function (rowIndex, node, columnIndex) {
              // Header row
              if (rowIndex === 0) {
                return '#3498db'; // Blue header background
              }
              // Alternating body rows (zebra striping)
              return rowIndex % 2 === 0 ? null : '#f8f9fa'; // Every even row gets light gray
            }
          }
        }
      ],

      // Default style untuk seluruh dokumen - SAMA seperti maintenance history
      defaultStyle: {
        fontSize: 9,
        color: '#2c3e50'
      },

      // Style dictionary - SAMA seperti maintenance history
      styles: {
        // Company information styles
        companyName: {
          fontSize: 12,
          bold: true,
          color: '#2c3e50'
        },
        companySubtitle: {
          fontSize: 10,
          color: '#7f8c8d',
          italics: true
        },

        // Document title styles
        documentTitle: {
          fontSize: 16,
          bold: true,
          color: '#2c3e50'
        },
        pageInfo: {
          fontSize: 8,
          color: '#95a5a6'
        },

        // Filter and content styles
        filterInfo: {
          fontSize: 10,
          italics: true,
          color: '#7f8c8d',
          background: '#ecf0f1'
        },

        // Table styles
        tableExample: {
          margin: [0, 5, 0, 15]
        },

        // Table header style
        tableHeader: {
          bold: true,
          fontSize: 10,
          color: 'white',
          alignment: 'center'
        },

        // Table header center style (untuk kolom No.)
        tableHeaderCenter: {
          bold: true,
          fontSize: 10,
          color: 'white',
          alignment: 'center'
        },

        // Table cell styles
        tableCell: {
          fontSize: 9,
          color: '#2c3e50'
        },
        tableCellCenter: {
          fontSize: 9,
          color: '#2c3e50',
          alignment: 'center'
        },
        tableCellRight: {
          fontSize: 9,
          color: '#2c3e50',
          alignment: 'right'
        },

        // Footer styles
        footerLeft: {
          fontSize: 8,
          color: '#95a5a6'
        },
        footerRight: {
          fontSize: 8,
          color: '#95a5a6'
        }
      }
    };

    return docDefinition;
  }

  /**
   * Process table data untuk PDF export - OPTIMIZED untuk Portrait
   */
  function processTableDataForPDF() {
    var tableData = [];
    var $table = $('#craneUsageHistoryTable');

    if ($table.length === 0) {
      console.warn('Table not found for PDF export');
      return [['Tidak ada data untuk diekspor']];
    }

    // Process header - OPTIMIZED: Menggabungkan beberapa kolom untuk Portrait
    var headerRow = [];
    var skipColumns = []; // Kolom yang akan di-skip untuk menghemat space

    $table.find('thead th').each(function (index) {
      var $th = $(this);

      // Skip action column dan beberapa kolom yang tidak penting untuk PDF
      if (!$th.hasClass('action-column') && !$th.text().toLowerCase().includes('aksi')) {
        var headerText = $th.text().trim();

        // Kolom pertama jadi "No." dan center
        if (index === 0) {
          headerRow.push({
            text: 'No.',
            style: 'tableHeaderCenter'
          });
        }
        // Untuk menghemat space, kita bisa menggabungkan atau menyingkat header
        else if (headerText === 'Total Jam') {
          headerRow.push({
            text: 'Total',
            style: 'tableHeader'
          });
        } else if (headerText === 'Operating') {
          headerRow.push({
            text: 'Operating',
            style: 'tableHeader'
          });
        } else if (headerText === 'Standby') {
          headerRow.push({
            text: 'Standby',
            style: 'tableHeader'
          });
        } else if (headerText === 'Maintenance') {
          headerRow.push({
            text: 'Maint.',
            style: 'tableHeader'
          });
        } else if (headerText === 'Entries') {
          // Skip entries column untuk menghemat space
          skipColumns.push(index);
        } else {
          headerRow.push({
            text: headerText,
            style: 'tableHeader'
          });
        }
      } else {
        skipColumns.push(index);
      }
    });

    if (headerRow.length > 0) {
      tableData.push(headerRow);
    }

    // Process body rows
    $table.find('tbody tr').each(function () {
      var $row = $(this);
      var rowData = [];

      $row.find('td').each(function (index) {
        var $td = $(this);

        // Skip columns that we marked to skip
        if (skipColumns.indexOf(index) === -1) {
          var cellText = $td.text().trim();

          // Kolom pertama (No.) dibuat center
          if (index === 0) {
            rowData.push({
              text: cellText,
              style: 'tableCellCenter'
            });
          }
          // Format jam columns untuk lebih ringkas
          else if (cellText.includes(' jam')) {
            rowData.push(cellText.replace(' jam', ''));
          }
          // Format entries untuk lebih ringkas
          else if (cellText.includes(' entries')) {
            // Skip entries column
          } else {
            // Kolom lainnya simple string
            rowData.push(cellText);
          }
        }
      });

      if (rowData.length > 0) {
        tableData.push(rowData);
      }
    });

    // Jika tidak ada data
    if (tableData.length <= 1) {
      tableData.push([
        {
          text: 'Tidak ada data yang tersedia',
          colSpan: headerRow.length || 9,
          alignment: 'center',
          style: 'tableCell'
        }
      ]);
    }

    return tableData;
  }

  // ===== DATATABLE FUNCTIONS =====
  /**
   * Initialize DataTable for export functionality dengan perbaikan pdfMake
   */
  function initDataTable() {
    // Destroy previous instance if exists
    if (dataTable) {
      dataTable.destroy();
    }

    dataTable = $('#craneUsageHistoryTable').DataTable({
      dom: 'Bfrtip',
      paging: false,
      ordering: false,
      info: false,
      searching: false,
      buttons: [
        {
          extend: 'excelHtml5',
          text: '<i class="bx bx-file me-1"></i> Excel',
          className: 'btn btn-sm btn-success hidden-button',
          title: config.exportTitle,
          exportOptions: {
            columns: ':not(.action-column):not(:last-child)'
          }
        },
        {
          extend: 'pdfHtml5',
          text: '<i class="bx bx-file me-1"></i> PDF',
          className: 'btn btn-sm btn-danger hidden-button',
          title: config.exportTitle,
          exportOptions: {
            columns: ':not(.action-column):not(:last-child)'
          },
          customize: function (doc) {
            try {
              // Get processed table data
              var tableData = processTableDataForPDF();

              // Create new document definition
              var newDocDef = createDocumentDefinition(tableData);

              // Replace the entire doc object properties
              Object.keys(doc).forEach(function (key) {
                delete doc[key];
              });

              Object.keys(newDocDef).forEach(function (key) {
                doc[key] = newDocDef[key];
              });

              console.log('PDF document definition created successfully');
            } catch (error) {
              console.error('Error in PDF customize function:', error);

              // Fallback: minimal PDF structure
              doc.content = [
                {
                  text: config.exportTitle,
                  style: { fontSize: 16, bold: true, alignment: 'center', margin: [0, 0, 0, 20] }
                },
                {
                  text: 'Terjadi kesalahan dalam pembuatan PDF. Data mungkin tidak lengkap.',
                  style: { fontSize: 12, alignment: 'center', color: 'red' }
                }
              ];
            }
          }
        }
      ]
    });

    return dataTable;
  }

  /**
   * Create and add export buttons
   */
  function setupExportButtons() {
    var exportContainer = $('#' + config.exportContainerId);
    if (exportContainer.length === 0) {
      console.warn('Export container not found');
      return;
    }

    // Hapus tombol yang ada sebelumnya untuk menghindari duplikasi
    exportContainer.empty();

    // Tombol Excel
    var excelButton = $(
      '<button type="button" class="btn btn-outline-secondary">' +
      '<i class="bx bxs-spreadsheet me-1"></i> Excel' +
      '</button>'
    );
    excelButton.on('click', function (e) {
      e.preventDefault();
      try {
        if (dataTable) {
          dataTable.button('.buttons-excel').trigger();
        }
      } catch (error) {
        showExportError('Excel', error.message);
      }
    });

    // Tombol PDF
    var pdfButton = $(
      '<button type="button" class="btn btn-outline-secondary">' +
      '<i class="bx bxs-file-pdf me-1"></i> PDF' +
      '</button>'
    );
    pdfButton.on('click', function (e) {
      e.preventDefault();
      try {
        if (dataTable) {
          dataTable.button('.buttons-pdf').trigger();
        }
      } catch (error) {
        showExportError('PDF', error.message);
      }
    });

    // Tambahkan tombol ke kontainer
    exportContainer.append(excelButton).append(pdfButton);

    // CSS penting untuk menyembunyikan tombol asli DataTables dan mengatur border
    if (!$('#datatables-export-style').length) {
      $(
        '<style id="datatables-export-style">' +
          '.hidden-button, .dt-buttons { display: none !important; }' +
          '#craneUsageHistoryTable thead tr:first-child th { border-top: 1px solid #B2B2B2 !important; }' +
          '#craneUsageHistoryTable tbody tr:last-child td { border-bottom: none !important; }' +
          '</style>'
      ).appendTo('head');
    }
  }

  /**
   * Show export error message
   */
  function showExportError(exportType, errorMessage) {
    var errorHtml =
      '<div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">' +
      '<i class="bx bx-error-circle me-2"></i>' +
      '<strong>Export ' +
      exportType +
      ' Gagal!</strong><br>' +
      'Terjadi kesalahan: ' +
      errorMessage +
      '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
      '</div>';

    // Show error in export container or table container
    var $container = $('#' + config.exportContainerId).parent();
    if ($container.length === 0) {
      $container = $('#' + config.tableContainerId);
    }

    $container.prepend(errorHtml);

    // Auto remove after 10 seconds
    setTimeout(function () {
      $container.find('.alert-danger').fadeOut('slow', function () {
        $(this).remove();
      });
    }, 10000);
  }

  // ===== AJAX & UI FUNCTIONS =====
  /**
   * Load table data via AJAX
   */
  function loadTableData() {
    var formData = $('#' + config.formId).serialize();
    console.log('Loading table with params:', formData);

    // Show loading indicator
    $('#tableLoadingOverlay').removeClass('d-none');

    $.ajax({
      url: config.getTableUrl,
      type: 'GET',
      data: formData,
      cache: false,
      success: function (response) {
        // Replace table container with new HTML
        $('#' + config.tableContainerId).html(response);

        // Initialize DataTable for export after content is loaded
        setTimeout(function () {
          try {
            initDataTable();
          } catch (error) {
            console.error('DataTable initialization error:', error);
          }
        }, 100);

        // Update URL to allow bookmarking state
        updateBrowserUrl();
      },
      error: function (xhr, status, error) {
        // Show error message
        $('#' + config.tableContainerId).html(
          '<div class="alert alert-danger m-3">' +
            '<i class="bx bx-error-circle me-2"></i>' +
            'Terjadi kesalahan saat memuat data. Silakan coba lagi.' +
            '<br><small class="text-muted">' +
            (xhr.responseText || error) +
            '</small>' +
            '</div>'
        );
        console.error('AJAX Error:', status, error);
      },
      complete: function () {
        // Hide loading indicator
        $('#tableLoadingOverlay').addClass('d-none');
      }
    });
  }

  /**
   * Update browser URL without page reload
   */
  function updateBrowserUrl() {
    if (window.history && window.history.pushState) {
      var formData = $('#' + config.formId).serialize();
      var baseUrl = window.location.href.split('?')[0];
      var newUrl = baseUrl + (formData ? '?' + formData : '');
      window.history.pushState({ path: newUrl }, '', newUrl);
    }
  }

  /**
   * Reset form and load fresh data
   */
  function resetFilters() {
    // Reset form
    document.getElementById(config.formId).reset();

    // Reset hidden fields
    $('#' + config.pageNumberId).val(1);

    // Load fresh data
    loadTableData();
  }

  /**
   * Initialize event handlers
   */
  function bindEvents() {
    // Table row click for navigation
    $(document).on('click', '.clickable-row', function (e) {
      if (!$(e.target).closest('.action-buttons-cell').length) {
        window.location = $(this).data('url');
      }
    });

    // Pagination click
    $(document).on('click', '.pagination-group a', function (e) {
      e.preventDefault();

      if ($(this).hasClass('disabled') || $(this).attr('aria-disabled') === 'true') {
        return false;
      }

      var page = $(this).data('page');
      $('#' + config.pageNumberId).val(page);
      loadTableData();
    });

    // Page size change
    $(document).on('change', '#pageSizeSelector', function () {
      $('#' + config.pageSizeId).val($(this).val());
      $('#' + config.pageNumberId).val(1); // Reset to page 1
      loadTableData();
    });

    // Filter inputs change
    $(document).on('change', config.filterInputSelector, function () {
      $('#' + config.pageNumberId).val(1); // Reset to page 1
      loadTableData();
    });

    // Search input with debounce
    $(document).on('keyup', '#' + config.searchInputId, function () {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(function () {
        $('#' + config.pageNumberId).val(1); // Reset to page 1
        loadTableData();
      }, 500);
    });

    // Reset button
    $('#' + config.resetBtnId).on('click', function (e) {
      e.preventDefault();
      resetFilters();
    });
  }

  // ===== PUBLIC API =====
  return {
    /**
     * Initialize the crane usage history page
     * @param {Object} options - Configuration options
     */
    init: function (options) {
      // Merge options with defaults
      config = $.extend(config, options || {});

      // Check required dependencies
      if (typeof window.pdfMake === 'undefined') {
        console.warn('pdfMake is not loaded. PDF export may not work.');
      }

      // Initialize export buttons
      setupExportButtons();

      // Initial DataTable setup
      try {
        initDataTable();
      } catch (error) {
        console.error('Initial DataTable setup error:', error);
      }

      // Bind events
      bindEvents();

      // Auto-close alerts after 5 seconds
      setTimeout(function () {
        $('.auto-close-alert').fadeOut('slow', function () {
          $(this).remove();
        });
      }, 5000);

      console.log('CraneUsageHistory initialized successfully');
    },

    /**
     * Manually reload the table data
     */
    reloadTable: function () {
      loadTableData();
    },

    /**
     * Update company logo dengan format yang benar
     * @param {string} logoData - Base64 encoded logo, SVG string, atau data URI
     */
    setCompanyLogo: function (logoData) {
      setCompanyLogo(logoData);
    },

    /**
     * Update company name
     * @param {string} companyName - Company name
     */
    setCompanyName: function (companyName) {
      config.companyName = companyName;
    },

    /**
     * Get current configuration
     */
    getConfig: function () {
      return config;
    },

    /**
     * Test PDF generation (untuk debugging)
     */
    testPDFGeneration: function () {
      try {
        var tableData = processTableDataForPDF();
        var docDef = createDocumentDefinition(tableData);

        console.log('PDF Document Definition:', docDef);

        if (window.pdfMake) {
          window.pdfMake.createPdf(docDef).download('test-crane-usage-history.pdf');
        } else {
          console.error('pdfMake is not available');
        }
      } catch (error) {
        console.error('Test PDF generation failed:', error);
      }
    },

    /**
     * Set logo dari URL
     * @param {string} logoUrl - URL to logo image
     */
    setCompanyLogoFromUrl: function (logoUrl) {
      fetch(logoUrl)
        .then(response => response.blob())
        .then(blob => {
          const reader = new FileReader();
          reader.onload = function () {
            setCompanyLogo(reader.result); // Data URI format
          };
          reader.readAsDataURL(blob);
        })
        .catch(error => {
          console.error('Error loading logo from URL:', error);
        });
    }
  };
})();
