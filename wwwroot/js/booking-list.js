/**
 * BookingList module - Complete Implementation
 * Manages the booking list page with AJAX loading, filtering, pagination and export
 */
var BookingList = (function () {
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
    exportTitle: 'Daftar Booking Crane',
    companyName: 'PT. KALTIM PRIMA COAL',
    currentUser: 'System',
    companyLogo: '',
    logoType: 'svg'
  };

  var dataTable = null;
  var searchTimeout = null;

  // ===== LOGO HANDLING FUNCTIONS =====
  function setCompanyLogo(logoData) {
    if (!logoData) {
      config.companyLogo = '';
      config.logoType = null;
      return;
    }

    if (logoData.startsWith('data:')) {
      config.companyLogo = logoData;
      config.logoType = 'image';
    } else if (logoData.startsWith('PHN2Zy')) {
      try {
        config.companyLogo = atob(logoData);
        config.logoType = 'svg';
      } catch (error) {
        console.error('Error decoding SVG base64:', error);
        config.companyLogo = 'data:image/svg+xml;base64,' + logoData;
        config.logoType = 'image';
      }
    } else if (logoData.includes('<svg')) {
      config.companyLogo = logoData;
      config.logoType = 'svg';
    } else {
      config.companyLogo = 'data:image/png;base64,' + logoData;
      config.logoType = 'image';
    }

    console.log('Logo set successfully. Type:', config.logoType);
  }

  // ===== PDF FUNCTIONS =====
  function getFilterInfo() {
    var crane = $('#CraneId option:selected').text();
    var department = $('#Department option:selected').text();
    var status = $('#Status option:selected').text();
    var startDate = $('#StartDate').val();
    var endDate = $('#EndDate').val();
    var search = $('#GlobalSearch').val();
    var filterParts = [];

    if (crane && crane !== '-- Semua Crane --') {
      filterParts.push('Crane: ' + crane);
    }

    if (department && department !== '-- Semua Departemen --') {
      filterParts.push('Departemen: ' + department);
    }

    if (status && status !== '-- Semua Status --') {
      filterParts.push('Status: ' + status);
    }

    if (startDate && endDate) {
      filterParts.push('Periode: ' + startDate + ' s/d ' + endDate);
    } else if (startDate) {
      filterParts.push('Dari: ' + startDate);
    } else if (endDate) {
      filterParts.push('Sampai: ' + endDate);
    }

    if (search) {
      filterParts.push('Pencarian: "' + search + '"');
    }

    return filterParts.length > 0 ? filterParts.join(' | ') : 'Semua Data';
  }

  function createDocumentDefinition(tableData) {
    var currentFilterInfo = getFilterInfo();
    var currentDate = new Date().toLocaleDateString('id-ID', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });

    var docDefinition = {
      pageSize: 'A4',
      pageOrientation: 'landscape', // Landscape untuk booking list karena lebih banyak kolom
      pageMargins: [40, config.companyLogo ? 100 : 80, 40, 60],

      info: {
        title: config.exportTitle,
        author: config.companyName,
        subject: 'Laporan Daftar Booking Crane',
        creator: 'Crane Booking System'
      },

      header: function (currentPage, pageCount, pageSize) {
        var headerContent = {
          columns: [],
          margin: [40, 20, 40, 20]
        };

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

          headerContent.columns.push({
            width: '*',
            text: ''
          });
        }

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

      content: [
        {
          text: 'Filter: ' + currentFilterInfo,
          style: 'filterInfo',
          margin: [0, 0, 0, 15]
        },

        {
          style: 'tableExample',
          table: {
            headerRows: 1,
            widths: [30, 'auto', '*', 'auto', '*', 'auto', 'auto', '*'], // Adjusted for booking columns
            body: tableData
          },
          layout: {
            fillColor: function (rowIndex, node, columnIndex) {
              if (rowIndex === 0) {
                return '#3498db';
              }
              return rowIndex % 2 === 0 ? null : '#f8f9fa';
            }
          }
        }
      ],

      defaultStyle: {
        fontSize: 9,
        color: '#2c3e50'
      },

      styles: {
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
        documentTitle: {
          fontSize: 16,
          bold: true,
          color: '#2c3e50'
        },
        pageInfo: {
          fontSize: 8,
          color: '#95a5a6'
        },
        filterInfo: {
          fontSize: 10,
          italics: true,
          color: '#7f8c8d',
          background: '#ecf0f1'
        },
        tableExample: {
          margin: [0, 5, 0, 15]
        },
        tableHeader: {
          bold: true,
          fontSize: 10,
          color: 'white',
          alignment: 'center'
        },
        tableHeaderCenter: {
          bold: true,
          fontSize: 10,
          color: 'white',
          alignment: 'center'
        },
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

  function processTableDataForPDF() {
    var tableData = [];
    var $table = $('#bookingListTable');

    if ($table.length === 0) {
      console.warn('Table not found for PDF export');
      return [['Tidak ada data untuk diekspor']];
    }

    // Process header
    var headerRow = [];
    $table.find('thead th').each(function (index) {
      var $th = $(this);

      if (!$th.hasClass('action-column') && !$th.text().toLowerCase().includes('aksi')) {
        var headerText = $th.text().trim();

        if (index === 0) {
          headerRow.push({
            text: 'No.',
            style: 'tableHeaderCenter'
          });
        } else {
          headerRow.push({
            text: headerText,
            style: 'tableHeader'
          });
        }
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

        if (!$td.hasClass('action-column') && !$td.hasClass('action-buttons-cell')) {
          var cellText = $td.text().trim();

          if (index === 0) {
            rowData.push({
              text: cellText,
              style: 'tableCellCenter'
            });
          } else {
            rowData.push(cellText);
          }
        }
      });

      if (rowData.length > 0) {
        tableData.push(rowData);
      }
    });

    if (tableData.length <= 1) {
      tableData.push([
        {
          text: 'Tidak ada data yang tersedia',
          colSpan: headerRow.length || 8,
          alignment: 'center',
          style: 'tableCell'
        }
      ]);
    }

    return tableData;
  }

  // ===== DATATABLE FUNCTIONS =====
  function initDataTable() {
    if (dataTable) {
      dataTable.destroy();
    }

    dataTable = $('#bookingListTable').DataTable({
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
              var tableData = processTableDataForPDF();
              var newDocDef = createDocumentDefinition(tableData);

              Object.keys(doc).forEach(function (key) {
                delete doc[key];
              });

              Object.keys(newDocDef).forEach(function (key) {
                doc[key] = newDocDef[key];
              });

              console.log('PDF document definition created successfully');
            } catch (error) {
              console.error('Error in PDF customize function:', error);

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

  function setupExportButtons() {
    $('#' + config.exportContainerId).empty();

    var buttonGroup = $('<div class="btn-group" role="group"></div>');

    var excelBtn = $(
      '<button type="button" class="btn btn-sm btn-success">' + '<i class="bx bx-file me-1"></i> Excel</button>'
    );
    var pdfBtn = $(
      '<button type="button" class="btn btn-sm btn-danger">' + '<i class="bx bx-file me-1"></i> PDF</button>'
    );

    buttonGroup.append(excelBtn).append(pdfBtn);
    $('#' + config.exportContainerId).append(buttonGroup);

    excelBtn.on('click', function (e) {
      e.preventDefault();
      if (dataTable) {
        try {
          dataTable.button(0).trigger();
        } catch (error) {
          console.error('Excel export error:', error);
          showExportError('Excel', error.message);
        }
      }
    });

    pdfBtn.on('click', function (e) {
      e.preventDefault();
      if (dataTable) {
        try {
          if (typeof window.pdfMake === 'undefined') {
            throw new Error('pdfMake library is not loaded');
          }

          dataTable.button(1).trigger();
        } catch (error) {
          console.error('PDF export error:', error);
          showExportError('PDF', error.message);
        }
      }
    });

    if (!$('#datatables-export-style').length) {
      $(
        '<style id="datatables-export-style">' +
          '.hidden-button, .dt-buttons { display: none !important; }' +
          '.btn-group .btn { border-radius: 0; }' +
          '.btn-group .btn:first-child { border-top-left-radius: 0.375rem; border-bottom-left-radius: 0.375rem; }' +
          '.btn-group .btn:last-child { border-top-right-radius: 0.375rem; border-bottom-right-radius: 0.375rem; }' +
          '.btn-group .btn:hover { transform: translateY(-1px); box-shadow: 0 2px 4px rgba(0,0,0,0.1); }' +
          '#bookingListTable thead tr:first-child th { border-top: 1px solid #B2B2B2 !important; }' +
          '#bookingListTable tbody tr:last-child td { border-bottom: none !important; }' +
          '</style>'
      ).appendTo('head');
    }
  }

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

    var $container = $('#' + config.exportContainerId).parent();
    if ($container.length === 0) {
      $container = $('#' + config.tableContainerId);
    }

    $container.prepend(errorHtml);

    setTimeout(function () {
      $container.find('.alert-danger').fadeOut('slow', function () {
        $(this).remove();
      });
    }, 10000);
  }

  // ===== AJAX & UI FUNCTIONS =====
  function loadTableData() {
    var formData = $('#' + config.formId).serialize();
    console.log('Loading table with params:', formData);

    $('#tableLoadingOverlay').removeClass('d-none');

    $.ajax({
      url: config.getTableUrl,
      type: 'GET',
      data: formData,
      cache: false,
      success: function (response) {
        $('#' + config.tableContainerId).html(response);

        setTimeout(function () {
          try {
            initDataTable();
          } catch (error) {
            console.error('DataTable initialization error:', error);
          }
        }, 100);

        updateBrowserUrl();
      },
      error: function (xhr, status, error) {
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
        $('#tableLoadingOverlay').addClass('d-none');
      }
    });
  }

  function updateBrowserUrl() {
    if (window.history && window.history.pushState) {
      var formData = $('#' + config.formId).serialize();
      var baseUrl = window.location.href.split('?')[0];
      var newUrl = baseUrl + (formData ? '?' + formData : '');
      window.history.pushState({ path: newUrl }, '', newUrl);
    }
  }

  function resetFilters() {
    document.getElementById(config.formId).reset();
    $('#' + config.pageNumberId).val(1);
    loadTableData();
  }

  function bindEvents() {
    $(document).on('click', '.clickable-row', function (e) {
      if (!$(e.target).closest('.action-buttons-cell').length) {
        window.location = $(this).data('url');
      }
    });

    $(document).on('click', '.pagination-group a', function (e) {
      e.preventDefault();

      if ($(this).hasClass('disabled') || $(this).attr('aria-disabled') === 'true') {
        return false;
      }

      var page = $(this).data('page');
      $('#' + config.pageNumberId).val(page);
      loadTableData();
    });

    $(document).on('change', '#pageSizeSelector', function () {
      $('#' + config.pageSizeId).val($(this).val());
      $('#' + config.pageNumberId).val(1);
      loadTableData();
    });

    $(document).on('change', config.filterInputSelector, function () {
      $('#' + config.pageNumberId).val(1);
      loadTableData();
    });

    $(document).on('keyup', '#' + config.searchInputId, function () {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(function () {
        $('#' + config.pageNumberId).val(1);
        loadTableData();
      }, 500);
    });

    $('#' + config.resetBtnId).on('click', function (e) {
      e.preventDefault();
      resetFilters();
    });
  }

  // ===== PUBLIC API =====
  return {
    init: function (options) {
      config = $.extend(config, options || {});

      if (typeof window.pdfMake === 'undefined') {
        console.warn('pdfMake is not loaded. PDF export may not work.');
      }

      setupExportButtons();

      try {
        initDataTable();
      } catch (error) {
        console.error('Initial DataTable setup error:', error);
      }

      bindEvents();

      setTimeout(function () {
        $('.auto-close-alert').fadeOut('slow', function () {
          $(this).remove();
        });
      }, 5000);

      console.log('BookingList initialized successfully');
    },

    reloadTable: function () {
      loadTableData();
    },

    setCompanyLogo: function (logoData) {
      setCompanyLogo(logoData);
    },

    setCompanyName: function (companyName) {
      config.companyName = companyName;
    },

    getConfig: function () {
      return config;
    },

    testPDFGeneration: function () {
      try {
        var tableData = processTableDataForPDF();
        var docDef = createDocumentDefinition(tableData);

        console.log('PDF Document Definition:', docDef);

        if (window.pdfMake) {
          window.pdfMake.createPdf(docDef).download('test-booking-list.pdf');
        } else {
          console.error('pdfMake is not available');
        }
      } catch (error) {
        console.error('Test PDF generation failed:', error);
      }
    },

    setCompanyLogoFromUrl: function (logoUrl) {
      fetch(logoUrl)
        .then(response => response.blob())
        .then(blob => {
          const reader = new FileReader();
          reader.onload = function () {
            setCompanyLogo(reader.result);
          };
          reader.readAsDataURL(blob);
        })
        .catch(error => {
          console.error('Error loading logo from URL:', error);
        });
    }
  };
})();
