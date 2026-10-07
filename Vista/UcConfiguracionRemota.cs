using AppIsp.Controlador;
using AppIsp.Modelo;

namespace AppIsp.Vista;

/// <summary>
/// Configuración del router. La ve el técnico y el maestro.
/// </summary>
public class UcConfiguracionRemota : PantallaBase, IPantallaRecargable
{
    private readonly ComboBox _router = InterfazAyuda.Lista();
    private readonly TextBox _w24Ssid = InterfazAyuda.Caja();
    private readonly TextBox _w24Clave = InterfazAyuda.Caja();
    private readonly ComboBox _w24Cifrado = InterfazAyuda.Lista();
    private readonly CheckBox _w24Activa = InterfazAyuda.Casilla("Red 2.4 GHz activa");
    private readonly TextBox _w5Ssid = InterfazAyuda.Caja();
    private readonly TextBox _w5Clave = InterfazAyuda.Caja();
    private readonly ComboBox _w5Cifrado = InterfazAyuda.Lista();
    private readonly CheckBox _w5Activa = InterfazAyuda.Casilla("Red 5 GHz activa");

    private readonly DataGridView _puertos = InterfazAyuda.Grilla();
    private readonly TextBox _puertoNombre = InterfazAyuda.Caja();
    private readonly ComboBox _protocolo = InterfazAyuda.Lista();
    private readonly NumericUpDown _externo = InterfazAyuda.Numero(65535, 0);
    private readonly NumericUpDown _interno = InterfazAyuda.Numero(65535, 0);
    private readonly TextBox _ipDestino = InterfazAyuda.Caja();
    private readonly CheckBox _puertoActivo = InterfazAyuda.Casilla("Regla activa");
    private int _idPuerto;

    private readonly CheckBox _dmz = InterfazAyuda.Casilla("DMZ activa");
    private readonly TextBox _dmzIp = InterfazAyuda.Caja();
    private readonly CheckBox _firewall = InterfazAyuda.Casilla("Firewall activo");
    private readonly CheckBox _spi = InterfazAyuda.Casilla("Inspección SPI");
    private readonly CheckBox _wanPing = InterfazAyuda.Casilla("Bloquear ping desde WAN");
    private readonly CheckBox _upnp = InterfazAyuda.Casilla("UPnP");
    private readonly CheckBox _accesoRemoto = InterfazAyuda.Casilla("Acceso remoto de administración");
    private readonly NumericUpDown _puertoAdmin = InterfazAyuda.Numero(65535, 0);
    private readonly ComboBox _filtroModo = InterfazAyuda.Lista();

    private readonly DataGridView _macs = InterfazAyuda.Grilla();
    private readonly TextBox _mac = InterfazAyuda.Caja();
    private readonly TextBox _macDesc = InterfazAyuda.Caja();
    private int _idMac;

    private readonly DataGridView _dispositivos = InterfazAyuda.Grilla();
    private readonly TextBox _dispNombre = InterfazAyuda.Caja();
    private readonly TextBox _dispMac = InterfazAyuda.Caja();
    private readonly TextBox _dispIp = InterfazAyuda.Caja();
    private readonly ComboBox _banda = InterfazAyuda.Lista();
    private readonly CheckBox _conectado = InterfazAyuda.Casilla("Conectado ahora");
    private int _idDisp;

    private readonly DataGridView _reservas = InterfazAyuda.Grilla();
    private readonly TextBox _resNombre = InterfazAyuda.Caja();
    private readonly TextBox _resMac = InterfazAyuda.Caja();
    private readonly TextBox _resIp = InterfazAyuda.Caja();
    private int _idReserva;

    private readonly ComboBox _ipv4Modo = InterfazAyuda.Lista();
    private readonly TextBox _ipv4 = InterfazAyuda.Caja();
    private readonly TextBox _mascara = InterfazAyuda.Caja();
    private readonly TextBox _puerta4 = InterfazAyuda.Caja();
    private readonly TextBox _dhcpIni = InterfazAyuda.Caja();
    private readonly TextBox _dhcpFin = InterfazAyuda.Caja();
    private readonly CheckBox _ipv6Activa = InterfazAyuda.Casilla("IPv6 activo");
    private readonly ComboBox _ipv6Modo = InterfazAyuda.Lista();
    private readonly TextBox _ipv6 = InterfazAyuda.Caja();
    private readonly TextBox _prefijo = InterfazAyuda.Caja();
    private readonly TextBox _puerta6 = InterfazAyuda.Caja();
    private readonly TextBox _dns1 = InterfazAyuda.Caja();
    private readonly TextBox _dns2 = InterfazAyuda.Caja();
    private readonly TextBox _dns61 = InterfazAyuda.Caja();
    private readonly TextBox _dns62 = InterfazAyuda.Caja();

    private bool _sincronizando;

    public UcConfiguracionRemota() : base(
        "Configuración remota",
        "Wi-Fi, puertos, DMZ, seguridad, filtrado MAC, dispositivos, reservas, IP y DNS del router elegido.")
    {
        _w24Clave.UseSystemPasswordChar = true;
        _w5Clave.UseSystemPasswordChar = true;
        _externo.Minimum = 1;
        _interno.Minimum = 1;
        _puertoAdmin.Minimum = 1;
        _puertoAdmin.Value = 80;
        _externo.Value = 80;
        _interno.Value = 80;
        InterfazAyuda.CargarValores(_w24Cifrado, ConfiguracionRedControlador.Cifrados);
        InterfazAyuda.CargarValores(_w5Cifrado, ConfiguracionRedControlador.Cifrados);
        InterfazAyuda.CargarValores(_protocolo, ConfiguracionRedControlador.Protocolos);
        InterfazAyuda.CargarValores(_filtroModo, ConfiguracionRedControlador.FiltrosMac);
        InterfazAyuda.CargarValores(_banda, ConfiguracionRedControlador.Bandas);
        InterfazAyuda.CargarValores(_ipv4Modo, ConfiguracionRedControlador.ModosIpv4);
        InterfazAyuda.CargarValores(_ipv6Modo, ConfiguracionRedControlador.ModosIpv6);
        InterfazAyuda.Pista(_mac, "AA:BB:CC:DD:EE:FF");
        InterfazAyuda.Pista(_dispMac, "AA:BB:CC:DD:EE:FF");
        InterfazAyuda.Pista(_resMac, "AA:BB:CC:DD:EE:FF");
        _firewall.Checked = true;
        _spi.Checked = true;
        _wanPing.Checked = true;
        _w24Activa.Checked = true;
        _w5Activa.Checked = true;
        _conectado.Checked = true;
        _puertoActivo.Checked = true;

        var barra = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = TemaVisual.Fondo };
        var etiqueta = new Label
        {
            Text = "Router",
            AutoSize = true,
            Location = new Point(0, 14),
            ForeColor = TemaVisual.TextoSuave
        };
        _router.Location = new Point(64, 8);
        _router.Width = 420;
        _router.SelectedIndexChanged += (_, _) => { if (!_sincronizando) CargarRouter(); };
        barra.Controls.Add(_router);
        barra.Controls.Add(etiqueta);

        var pestanas = new TabControl { Dock = DockStyle.Fill, Font = TemaVisual.Fuente };
        pestanas.TabPages.Add(PaginaWifi());
        pestanas.TabPages.Add(PaginaPuertos());
        pestanas.TabPages.Add(PaginaSeguridad());
        pestanas.TabPages.Add(PaginaMac());
        pestanas.TabPages.Add(PaginaDispositivos());
        pestanas.TabPages.Add(PaginaReservas());
        pestanas.TabPages.Add(PaginaIp());

        Cuerpo.Controls.Add(pestanas);
        Cuerpo.Controls.Add(barra);
    }

    public void Recargar()
    {
        try
        {
            _sincronizando = true;
            var id = InterfazAyuda.IdSeleccionado(_router);
            InterfazAyuda.CargarOpciones(_router, RouterControlador.ListarOpciones());
            if (id > 0) InterfazAyuda.SeleccionarId(_router, id);
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
        finally { _sincronizando = false; }
        CargarRouter();
    }

    private void CargarRouter()
    {
        var id = InterfazAyuda.IdSeleccionado(_router);
        if (id <= 0)
        {
            MostrarEstado("No hay routers para configurar.", true);
            return;
        }

        try
        {
            var config = ConfiguracionRedControlador.Leer(id);
            _w24Ssid.Text = config.Wifi24Ssid;
            _w24Clave.Text = config.Wifi24Clave;
            _w24Cifrado.SelectedValue = config.Wifi24Cifrado;
            _w24Activa.Checked = config.Wifi24Activa;
            _w5Ssid.Text = config.Wifi5Ssid;
            _w5Clave.Text = config.Wifi5Clave;
            _w5Cifrado.SelectedValue = config.Wifi5Cifrado;
            _w5Activa.Checked = config.Wifi5Activa;
            _dmz.Checked = config.DmzActiva;
            _dmzIp.Text = config.DmzIp ?? "";
            _firewall.Checked = config.FirewallActivo;
            _spi.Checked = config.SpiActivo;
            _wanPing.Checked = config.BloqueoWanPing;
            _upnp.Checked = config.UpnpActivo;
            _accesoRemoto.Checked = config.AccesoRemoto;
            _puertoAdmin.Value = Math.Clamp(config.PuertoAdmin, 1, 65535);
            _filtroModo.SelectedValue = config.FiltroMac;
            _ipv4Modo.SelectedValue = config.Ipv4Modo;
            _ipv4.Text = config.Ipv4Ip ?? "";
            _mascara.Text = config.Ipv4Mascara ?? "";
            _puerta4.Text = config.Ipv4Puerta ?? "";
            _dhcpIni.Text = config.Ipv4DhcpInicio ?? "";
            _dhcpFin.Text = config.Ipv4DhcpFin ?? "";
            _ipv6Activa.Checked = config.Ipv6Activa;
            _ipv6Modo.SelectedValue = config.Ipv6Modo;
            _ipv6.Text = config.Ipv6Ip ?? "";
            _prefijo.Text = config.Ipv6Prefijo ?? "";
            _puerta6.Text = config.Ipv6Puerta ?? "";
            _dns1.Text = config.DnsPrimario ?? "";
            _dns2.Text = config.DnsSecundario ?? "";
            _dns61.Text = config.Dns6Primario ?? "";
            _dns62.Text = config.Dns6Secundario ?? "";

            _puertos.DataSource = ConfiguracionRedControlador.LeerPuertos(id);
            InterfazAyuda.PrepararGrilla(_puertos,
                ("id_puerto", "Id", false),
                ("nombre", "Nombre", true),
                ("protocolo", "Protocolo", true),
                ("puerto_externo", "Externo", true),
                ("puerto_interno", "Interno", true),
                ("ip_destino", "IP destino", true),
                ("activo", "Activa", true));
            _macs.DataSource = ConfiguracionRedControlador.LeerMac(id);
            InterfazAyuda.PrepararGrilla(_macs,
                ("id_mac", "Id", false),
                ("mac", "MAC", true),
                ("descripcion", "Descripción", true));
            _dispositivos.DataSource = ConfiguracionRedControlador.LeerDispositivos(id);
            InterfazAyuda.PrepararGrilla(_dispositivos,
                ("id_dispositivo", "Id", false),
                ("nombre", "Nombre", true),
                ("mac", "MAC", true),
                ("ip", "IP", true),
                ("banda", "Banda", true),
                ("conectado", "Conectado", true));
            _reservas.DataSource = ConfiguracionRedControlador.LeerReservas(id);
            InterfazAyuda.PrepararGrilla(_reservas,
                ("id_reserva", "Id", false),
                ("nombre", "Nombre", true),
                ("mac", "MAC", true),
                ("ip", "IP reservada", true));
            MostrarEstado("Configuración cargada.");
        }
        catch (Exception ex) { InterfazAyuda.Error(ex); }
    }

    private ConfiguracionRouter TomarConfig()
    {
        return new ConfiguracionRouter
        {
            IdRouter = InterfazAyuda.IdSeleccionado(_router),
            Wifi24Ssid = _w24Ssid.Text,
            Wifi24Clave = _w24Clave.Text,
            Wifi24Cifrado = InterfazAyuda.ValorSeleccionado(_w24Cifrado),
            Wifi24Activa = _w24Activa.Checked,
            Wifi5Ssid = _w5Ssid.Text,
            Wifi5Clave = _w5Clave.Text,
            Wifi5Cifrado = InterfazAyuda.ValorSeleccionado(_w5Cifrado),
            Wifi5Activa = _w5Activa.Checked,
            DmzActiva = _dmz.Checked,
            DmzIp = _dmzIp.Text,
            FirewallActivo = _firewall.Checked,
            SpiActivo = _spi.Checked,
            BloqueoWanPing = _wanPing.Checked,
            UpnpActivo = _upnp.Checked,
            AccesoRemoto = _accesoRemoto.Checked,
            PuertoAdmin = (int)_puertoAdmin.Value,
            FiltroMac = InterfazAyuda.ValorSeleccionado(_filtroModo),
            Ipv4Modo = InterfazAyuda.ValorSeleccionado(_ipv4Modo),
            Ipv4Ip = _ipv4.Text,
            Ipv4Mascara = _mascara.Text,
            Ipv4Puerta = _puerta4.Text,
            Ipv4DhcpInicio = _dhcpIni.Text,
            Ipv4DhcpFin = _dhcpFin.Text,
            Ipv6Activa = _ipv6Activa.Checked,
            Ipv6Modo = InterfazAyuda.ValorSeleccionado(_ipv6Modo),
            Ipv6Ip = _ipv6.Text,
            Ipv6Prefijo = _prefijo.Text,
            Ipv6Puerta = _puerta6.Text,
            DnsPrimario = _dns1.Text,
            DnsSecundario = _dns2.Text,
            Dns6Primario = _dns61.Text,
            Dns6Secundario = _dns62.Text
        };
    }

    private void GuardarConfig()
    {
        Informar(ConfiguracionRedControlador.Guardar(TomarConfig()));
    }

    private TabPage PaginaWifi()
    {
        var pagina = new TabPage("Wi-Fi");
        var columnas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = TemaVisual.Fondo };
        columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columnas.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var izq = InterfazAyuda.ColumnaFormulario();
        var der = InterfazAyuda.ColumnaFormulario();
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.TituloFicha("Red 2.4 GHz"), 36);
        InterfazAyuda.AgregarFila(izq, _w24Activa, 36);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("Nombre (SSID)", _w24Ssid), 68);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("Contraseña", _w24Clave), 68);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("Cifrado", _w24Cifrado), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.TituloFicha("Red 5 GHz"), 36);
        InterfazAyuda.AgregarFila(der, _w5Activa, 36);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("Nombre (SSID)", _w5Ssid), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("Contraseña", _w5Clave), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("Cifrado", _w5Cifrado), 68);
        var ver = InterfazAyuda.Casilla("Mostrar contraseñas");
        ver.CheckedChanged += (_, _) => _w24Clave.UseSystemPasswordChar = _w5Clave.UseSystemPasswordChar = !ver.Checked;
        var guardar = InterfazAyuda.Boton("Guardar Wi-Fi", EstiloBoton.Primario);
        guardar.Click += (_, _) => GuardarConfig();
        InterfazAyuda.AgregarFila(izq, ver, 36);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.FilaBotones(guardar), 52);
        columnas.Controls.Add(izq, 0, 0);
        columnas.Controls.Add(der, 1, 0);
        pagina.Controls.Add(columnas);
        return pagina;
    }

    private TabPage PaginaPuertos()
    {
        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Reenvío de puertos"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Nombre", _puertoNombre), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Protocolo", _protocolo), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Puerto externo", _externo), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Puerto interno", _interno), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("IP de destino", _ipDestino), 68);
        InterfazAyuda.AgregarFila(form, _puertoActivo, 36);
        var nuevo = InterfazAyuda.Boton("Nueva", EstiloBoton.Secundario);
        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
        nuevo.Click += (_, _) =>
        {
            _idPuerto = 0;
            _puertoNombre.Clear();
            _ipDestino.Clear();
            _puertoActivo.Checked = true;
        };
        guardar.Click += (_, _) =>
        {
            Informar(ConfiguracionRedControlador.GuardarPuerto(
                _idPuerto, InterfazAyuda.IdSeleccionado(_router), _puertoNombre.Text,
                InterfazAyuda.ValorSeleccionado(_protocolo), (int)_externo.Value, (int)_interno.Value,
                _ipDestino.Text, _puertoActivo.Checked));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        eliminar.Click += (_, _) =>
        {
            if (!InterfazAyuda.Confirmar("¿Eliminar esta regla de puerto?")) return;
            Informar(ConfiguracionRedControlador.EliminarPuerto(_idPuerto, InterfazAyuda.IdSeleccionado(_router)));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(nuevo, guardar, eliminar), 56);
        _puertos.SelectionChanged += (_, _) =>
        {
            _idPuerto = InterfazAyuda.IdFila(_puertos, "id_puerto");
            if (_idPuerto == 0 || _puertos.CurrentRow == null) return;
            var fila = _puertos.CurrentRow;
            _puertoNombre.Text = Convert.ToString(fila.Cells["nombre"].Value) ?? "";
            _protocolo.SelectedValue = Convert.ToString(fila.Cells["protocolo"].Value) ?? "TCP";
            _externo.Value = Math.Clamp(Convert.ToInt32(fila.Cells["puerto_externo"].Value), 1, 65535);
            _interno.Value = Math.Clamp(Convert.ToInt32(fila.Cells["puerto_interno"].Value), 1, 65535);
            _ipDestino.Text = Convert.ToString(fila.Cells["ip_destino"].Value) ?? "";
            _puertoActivo.Checked = Convert.ToString(fila.Cells["activo"].Value) == "Sí";
        };
        return PaginaConGrilla("Puertos", _puertos, ficha);
    }

    private TabPage PaginaSeguridad()
    {
        var pagina = new TabPage("DMZ y seguridad");
        var form = InterfazAyuda.ColumnaFormulario();
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("DMZ"), 36);
        InterfazAyuda.AgregarFila(form, _dmz, 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("IP de la DMZ", _dmzIp), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Seguridad"), 36);
        InterfazAyuda.AgregarFila(form, _firewall, 32);
        InterfazAyuda.AgregarFila(form, _spi, 32);
        InterfazAyuda.AgregarFila(form, _wanPing, 32);
        InterfazAyuda.AgregarFila(form, _upnp, 32);
        InterfazAyuda.AgregarFila(form, _accesoRemoto, 32);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Puerto de administración", _puertoAdmin), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Filtrado MAC", _filtroModo), 68);
        var guardar = InterfazAyuda.Boton("Guardar seguridad", EstiloBoton.Primario);
        guardar.Click += (_, _) => GuardarConfig();
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(guardar), 56);
        pagina.Controls.Add(form);
        return pagina;
    }

    private TabPage PaginaMac()
    {
        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Lista de filtrado MAC"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("MAC", _mac), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Descripción", _macDesc), 68);
        var nuevo = InterfazAyuda.Boton("Nueva", EstiloBoton.Secundario);
        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
        nuevo.Click += (_, _) => { _idMac = 0; _mac.Clear(); _macDesc.Clear(); };
        guardar.Click += (_, _) =>
        {
            Informar(ConfiguracionRedControlador.GuardarMac(_idMac, InterfazAyuda.IdSeleccionado(_router), _mac.Text, _macDesc.Text));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        eliminar.Click += (_, _) =>
        {
            if (!InterfazAyuda.Confirmar("¿Quitar esta MAC del filtro?")) return;
            Informar(ConfiguracionRedControlador.EliminarMac(_idMac, InterfazAyuda.IdSeleccionado(_router)));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(nuevo, guardar, eliminar), 56);
        _macs.SelectionChanged += (_, _) =>
        {
            _idMac = InterfazAyuda.IdFila(_macs, "id_mac");
            if (_idMac == 0 || _macs.CurrentRow == null) return;
            _mac.Text = Convert.ToString(_macs.CurrentRow.Cells["mac"].Value) ?? "";
            _macDesc.Text = Convert.ToString(_macs.CurrentRow.Cells["descripcion"].Value) ?? "";
        };
        return PaginaConGrilla("Filtrado MAC", _macs, ficha);
    }

    private TabPage PaginaDispositivos()
    {
        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("Dispositivo conectado"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Nombre", _dispNombre), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("MAC", _dispMac), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("IP", _dispIp), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Banda", _banda), 68);
        InterfazAyuda.AgregarFila(form, _conectado, 36);
        var nuevo = InterfazAyuda.Boton("Nuevo", EstiloBoton.Secundario);
        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
        nuevo.Click += (_, _) =>
        {
            _idDisp = 0;
            _dispNombre.Clear();
            _dispMac.Clear();
            _dispIp.Clear();
            _conectado.Checked = true;
        };
        guardar.Click += (_, _) =>
        {
            Informar(ConfiguracionRedControlador.GuardarDispositivo(
                _idDisp, InterfazAyuda.IdSeleccionado(_router), _dispNombre.Text, _dispMac.Text, _dispIp.Text,
                InterfazAyuda.ValorSeleccionado(_banda), _conectado.Checked));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        eliminar.Click += (_, _) =>
        {
            if (!InterfazAyuda.Confirmar("¿Eliminar este dispositivo?")) return;
            Informar(ConfiguracionRedControlador.EliminarDispositivo(_idDisp, InterfazAyuda.IdSeleccionado(_router)));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(nuevo, guardar, eliminar), 56);
        _dispositivos.SelectionChanged += (_, _) =>
        {
            _idDisp = InterfazAyuda.IdFila(_dispositivos, "id_dispositivo");
            if (_idDisp == 0 || _dispositivos.CurrentRow == null) return;
            var fila = _dispositivos.CurrentRow;
            _dispNombre.Text = Convert.ToString(fila.Cells["nombre"].Value) ?? "";
            _dispMac.Text = Convert.ToString(fila.Cells["mac"].Value) ?? "";
            _dispIp.Text = Convert.ToString(fila.Cells["ip"].Value) ?? "";
            _banda.SelectedValue = Convert.ToString(fila.Cells["banda"].Value) ?? "LAN";
            _conectado.Checked = Convert.ToString(fila.Cells["conectado"].Value) == "Sí";
        };
        return PaginaConGrilla("Dispositivos", _dispositivos, ficha);
    }

    private TabPage PaginaReservas()
    {
        var ficha = InterfazAyuda.CrearFicha(out var form);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.TituloFicha("IP reservada"), 36);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("Nombre", _resNombre), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("MAC", _resMac), 68);
        InterfazAyuda.AgregarFila(form, InterfazAyuda.Bloque("IP", _resIp), 68);
        var nuevo = InterfazAyuda.Boton("Nueva", EstiloBoton.Secundario);
        var guardar = InterfazAyuda.Boton("Guardar", EstiloBoton.Primario);
        var eliminar = InterfazAyuda.Boton("Eliminar", EstiloBoton.Peligro);
        nuevo.Click += (_, _) => { _idReserva = 0; _resNombre.Clear(); _resMac.Clear(); _resIp.Clear(); };
        guardar.Click += (_, _) =>
        {
            Informar(ConfiguracionRedControlador.GuardarReserva(
                _idReserva, InterfazAyuda.IdSeleccionado(_router), _resNombre.Text, _resMac.Text, _resIp.Text));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        eliminar.Click += (_, _) =>
        {
            if (!InterfazAyuda.Confirmar("¿Eliminar esta reserva?")) return;
            Informar(ConfiguracionRedControlador.EliminarReserva(_idReserva, InterfazAyuda.IdSeleccionado(_router)));
            if (Estado.ForeColor == TemaVisual.Exito) CargarRouter();
        };
        InterfazAyuda.AgregarFila(form, InterfazAyuda.FilaBotones(nuevo, guardar, eliminar), 56);
        _reservas.SelectionChanged += (_, _) =>
        {
            _idReserva = InterfazAyuda.IdFila(_reservas, "id_reserva");
            if (_idReserva == 0 || _reservas.CurrentRow == null) return;
            _resNombre.Text = Convert.ToString(_reservas.CurrentRow.Cells["nombre"].Value) ?? "";
            _resMac.Text = Convert.ToString(_reservas.CurrentRow.Cells["mac"].Value) ?? "";
            _resIp.Text = Convert.ToString(_reservas.CurrentRow.Cells["ip"].Value) ?? "";
        };
        return PaginaConGrilla("IPs reservadas", _reservas, ficha);
    }

    private TabPage PaginaIp()
    {
        var pagina = new TabPage("IP y DNS");
        var columnas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = TemaVisual.Fondo };
        columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columnas.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var izq = InterfazAyuda.ColumnaFormulario();
        var der = InterfazAyuda.ColumnaFormulario();
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.TituloFicha("IPv4"), 36);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("Modo", _ipv4Modo), 68);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("Dirección", _ipv4), 68);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("Máscara", _mascara), 68);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("Puerta de enlace", _puerta4), 68);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("DHCP desde", _dhcpIni), 68);
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.Bloque("DHCP hasta", _dhcpFin), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.TituloFicha("IPv6"), 36);
        InterfazAyuda.AgregarFila(der, _ipv6Activa, 36);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("Modo", _ipv6Modo), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("Dirección", _ipv6), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("Prefijo", _prefijo), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("Puerta de enlace", _puerta6), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.TituloFicha("DNS"), 36);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("DNS IPv4 primario", _dns1), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("DNS IPv4 secundario", _dns2), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("DNS IPv6 primario", _dns61), 68);
        InterfazAyuda.AgregarFila(der, InterfazAyuda.Bloque("DNS IPv6 secundario", _dns62), 68);
        var guardar = InterfazAyuda.Boton("Guardar IP y DNS", EstiloBoton.Primario);
        guardar.Click += (_, _) => GuardarConfig();
        InterfazAyuda.AgregarFila(izq, InterfazAyuda.FilaBotones(guardar), 56);
        columnas.Controls.Add(izq, 0, 0);
        columnas.Controls.Add(der, 1, 0);
        pagina.Controls.Add(columnas);
        return pagina;
    }

    private static TabPage PaginaConGrilla(string titulo, DataGridView grilla, Control ficha)
    {
        var pagina = new TabPage(titulo);
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = TemaVisual.Fondo };
        panel.Controls.Add(InterfazAyuda.MarcoGrilla(grilla));
        panel.Controls.Add(ficha);
        pagina.Controls.Add(panel);
        return pagina;
    }
}
