// Dashboard charts. Data comes from the #dashboard-data JSON island the view
// renders; nothing here calls the server again.
(function () {
    'use strict';

    var dataElement = document.getElementById('dashboard-data');
    if (!dataElement) return;

    var data = JSON.parse(dataElement.textContent);

    // Honor the OS motion preference: no chart entrance animation when the
    // user asks for reduced motion.
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var animation = reduceMotion ? false : { duration: 600 };

    var palette = {
        blue: 'rgba(37, 140, 251, 1)',
        blueFill: 'rgba(37, 140, 251, 0.15)',
        green: 'rgba(25, 135, 84, 1)',
        greenFill: 'rgba(25, 135, 84, 0.35)',
        red: 'rgba(220, 53, 69, 1)',
        redFill: 'rgba(220, 53, 69, 0.35)',
        amber: 'rgba(253, 126, 20, 1)',
        amberFill: 'rgba(253, 126, 20, 0.35)',
        gray: 'rgba(108, 117, 125, 1)',
        grayFill: 'rgba(108, 117, 125, 0.35)'
    };

    // Bootstrap-style 5-class categorical fill for the donut.
    var doughnutPalette = ['#258cfb', '#198754', '#dc3545', '#fd7e14', '#6f42c1', '#20c997'];

    var defaultLegend = { labels: { boxWidth: 12, boxHeight: 12 } };

    function monthShort(iso) {
        var parts = iso.split('-');
        var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return months[Number(parts[1]) - 1] + ' ' + parts[0];
    }

    function thousands(value) {
        return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
    }

    function trendMonths() {
        // Points and Flows cover the same aligned window; fall back to Flows
        // if Points is empty (nothing ever hired yet).
        var source = data.trend && data.trend.points && data.trend.points.length
            ? data.trend.points
            : (data.trend && data.trend.flows) || [];
        // Serialized month values are ISO strings ("2024-11-01T00:00:00").
        return source.map(function (point) {
            return monthShort(point.month.slice(0, 10));
        });
    }

    function buildHeadcountChart(labels) {
        var ctx = document.getElementById('headcount-chart');
        if (!ctx) return;

        var points = data.trend && data.trend.points ? data.trend.points : [];

        new Chart(ctx.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Headcount',
                    data: points.map(function (p) { return p.headcount; }),
                    borderColor: palette.blue,
                    backgroundColor: palette.blueFill,
                    fill: true,
                    tension: 0.3,
                    pointRadius: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: animation,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    function buildFlowChart(labels) {
        var ctx = document.getElementById('flow-chart');
        if (!ctx) return;

        var flows = data.trend && data.trend.flows ? data.trend.flows : [];

        new Chart(ctx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Hires',
                        data: flows.map(function (f) { return f.hires; }),
                        backgroundColor: palette.greenFill,
                        borderColor: palette.green,
                        borderWidth: 1
                    },
                    {
                        label: 'Exits',
                        data: flows.map(function (f) { return f.exits; }),
                        backgroundColor: palette.redFill,
                        borderColor: palette.red,
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: animation,
                interaction: { mode: 'index', intersect: false },
                plugins: { legend: defaultLegend },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    function buildExitsChart() {
        var ctx = document.getElementById('exits-chart');
        if (!ctx) return;

        var exits = data.exitsByStatus || [];

        new Chart(ctx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: exits.map(function (e) { return e.status; }),
                datasets: [{
                    label: 'Exits',
                    data: exits.map(function (e) { return e.exits; }),
                    backgroundColor: palette.grayFill,
                    borderColor: palette.gray,
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                animation: animation,
                plugins: { legend: { display: false } },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    function buildTypeChart() {
        var ctx = document.getElementById('type-chart');
        if (!ctx) return;

        var shares = data.typeShares || [];

        new Chart(ctx.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: shares.map(function (s) { return s.type; }),
                datasets: [{
                    data: shares.map(function (s) { return s.headcount; }),
                    backgroundColor: doughnutPalette
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: animation,
                plugins: { legend: { position: 'right', labels: { boxWidth: 12, boxHeight: 12 } } }
            }
        });
    }

    function buildPayrollChart() {
        var ctx = document.getElementById('payroll-chart');
        if (!ctx) return;

        var payrolls = data.payrolls || [];

        new Chart(ctx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: payrolls.map(function (p) { return p.code; }),
                datasets: [{
                    label: 'Total salary',
                    data: payrolls.map(function (p) { return p.totalSalary; }),
                    backgroundColor: palette.blueFill,
                    borderColor: palette.blue,
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                animation: animation,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            afterBody: function (tooltipItem) {
                                var item = payrolls[tooltipItem[0].dataIndex];
                                if (!item) return '';
                                return 'Headcount: ' + item.headcount
                                    + '  |  Average: ' + thousands(item.averageSalary);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { callback: thousands, precision: 0 }
                    }
                }
            }
        });
    }

    function buildSpreadChart() {
        var ctx = document.getElementById('spread-chart');
        if (!ctx) return;

        var payrolls = data.payrolls || [];

        // Floating bars carry [min, max]; the average rides along in the
        // tooltip so compression is visible without a third dataset.
        new Chart(ctx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: payrolls.map(function (p) { return p.code; }),
                datasets: [{
                    label: 'Salary range',
                    data: payrolls.map(function (p) { return [p.minimumSalary, p.maximumSalary]; }),
                    backgroundColor: palette.amberFill,
                    borderColor: palette.amber,
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                animation: animation,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (tooltipItem) {
                                var item = payrolls[tooltipItem.dataIndex];
                                if (!item) return '';
                                return 'Min: ' + thousands(item.minimumSalary)
                                    + '  |  Max: ' + thousands(item.maximumSalary)
                                    + '  |  Avg: ' + thousands(item.averageSalary);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { callback: thousands, precision: 0 }
                    }
                }
            }
        });
    }

    var labels = trendMonths();

    buildHeadcountChart(labels);
    buildFlowChart(labels);
    buildExitsChart();
    buildTypeChart();
    buildPayrollChart();
    buildSpreadChart();
})();
