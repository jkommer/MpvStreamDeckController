// The OSD dropdown's options. After the "mpv default" entry, the values must match MpvOsdMode.All in the plugin, in the same order.
// PropertyInspectorTests checks this by reading the list between the osd-modes markers as JSON, so keep the markers and keep the list valid JSON.
const osdModes = /** osd-modes **/ [
    { "value": "", "label": "None (mpv default)" },
    { "value": "osd-auto", "label": "Auto" },
    { "value": "no-osd", "label": "Off" },
    { "value": "osd-bar", "label": "Bar" },
    { "value": "osd-msg", "label": "Message" },
    { "value": "osd-msg-bar", "label": "Message and bar" }
] /** /osd-modes **/;

// <mpv-osd-select>: the OSD setting. Replaces itself with the sdpi-item so it doesn't affect the layout.
customElements.define("mpv-osd-select", class extends HTMLElement {
    connectedCallback() {
        const item = document.createElement("sdpi-item");
        item.setAttribute("label", "OSD");

        const select = document.createElement("sdpi-select");
        select.setAttribute("setting", "osd");
        select.setAttribute("default", "");
        item.append(select);

        this.replaceWith(item);

        // sdpi-select only picks up options added after it's on the page (it watches for added children), so add them now.
        select.append(...osdModes.map(({ value, label }) => {
            const option = document.createElement("option");
            option.value = value;
            option.textContent = label;
            return option;
        }));
    }
});

function openUrl(url)
{
const { streamDeckClient } = SDPIComponents;
streamDeckClient.send('openUrl', {
    url: url
});
}