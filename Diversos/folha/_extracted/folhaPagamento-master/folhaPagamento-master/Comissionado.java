package folhaPagamento;

public class Comissionado extends Funcionario {
	double totVendas;
	double percentual;
	
	public Comissionado (String nome, String setor, String dataAdm, double totVendas, double percentual) {
		super(nome, setor, dataAdm);
		this.setTotVendas(totVendas);
		this.setPercentual(percentual);
	}
	
	public double getCalculoComissionado (double totVendas, double percentual) {
		double resultado;
		
		resultado = totVendas * percentual;
		
		return resultado;
	}

	public double getTotVendas() {
		return totVendas;
	}

	public void setTotVendas(double totVendas) {
		this.totVendas = totVendas;
	}

	public double getPercentual() {
		return percentual;
	}

	public void setPercentual(double percentual) {
		this.percentual = percentual;
	}
	
	
}
