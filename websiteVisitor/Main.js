function goToNextPage()
{
    const text = 'Hello, world!';
    const params = new URLSearchParams();
    params.append('text', text);
    const queryString = params.toString();
    const targetURL = 'results.html?' + queryString;
    window.location.href = targetURL;
}
function readInput()
{
    const inputElement = document.getElementById("userInput")
    const inputValue = inputElement.value;
    document.getElementById("abutton").textContent = "You entered: " + inputValue;
}
function downloadTxtFile() {
    const URLName = document.getElementById("GameURL");
    const TabName = document.getElementById("TabName");
    const TabDescrip = document.getElementById("TabDescrip");
    try {
        const response = await fetch("template.txt");
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        let templateContent = await response.text();

        // 3. Perform string replacements on the content
        templateContent = templateContent
            .replace('[TITLEREPLACE]', clientName)
            .replace('[CONTENTREPLACE]', currentDate)
            .replace('[URLREPLACE]', keyMetric)
            .replace('[[METRIC_VALUE]]', metricValue)
            .replace('[[RECOMMENDATION]]', recommendation);

        // 4. Create a Blob object from the customized content
        const blob = new Blob([templateContent], { type: 'text/plain' });

        // 5. Create a temporary download link (<a> element)
        const downloadLink = document.createElement('a');
        downloadLink.href = URL.createObjectURL(blob);
        downloadLink.download = filename;

        // 6. Programmatically click the link to trigger the download
        document.body.appendChild(downloadLink);
        downloadLink.click();

        // 7. Clean up
        document.body.removeChild(downloadLink);
        URL.revokeObjectURL(downloadLink.href);

        alert(`Custom file "${filename}" generated and downloaded for ${clientName}!`);

    } catch (error) {
        console.error('Error processing template file:', error);
        alert('Failed to load or process the template file.');
    }
}
    const textContent = '';
    const filename = 'generated_file.txt';
    const blob = new Blob([textContent], { type: 'text/plain' });
    const downloadLink = document.createElement('a');
    downloadLink.href = URL.createObjectURL(blob);
    downloadLink.download = filename;
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
    URL.revokeObjectURL(downloadLink.href);
    alert(`File "${filename}" has been generated and downloaded!`);
}